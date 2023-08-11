using SteamProperties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BWRWinforms.Utility;

namespace BWRWinforms
{
    internal class Reactor
    {
        readonly Form1 Form;

        public Reactor(Form1 form, Sim sim,  double baseTemperature)
        {
            Form = form;
            this.sim = sim;
            this.baseTemperature = baseTemperature;
            FuelTemp = baseTemperature;
            WaterTemp = baseTemperature;
            VesselTemp = baseTemperature;

            //These are mathed out to be fairly close... assuming the source I was working from is correct
            DelayedFraction = new double[] { .000215, .001424, .001274, .002568, .000748, .000273 };
            DelayedTickMult = new double[] { .998758, .996942, .988829, 0.97031, 0.8921, 0.73981 };
            promptFraction = 1 - DelayedFraction[0] - DelayedFraction[1] - DelayedFraction[2] - DelayedFraction[3] - DelayedFraction[4] - DelayedFraction[5];

            DecayHeatFraction = new double[] { 0.033248, 0.023273, 0.0066496, 0.0018286 };
            DecayHeatTickMult = new double[] { 0.995, 0.99993, 0.9999992, 0.9999999975 };
        }

        readonly Sim sim;

        const double minPower = .001;

        readonly double[] DelayedQuantity = new double[6];
        readonly double[] DelayedTickMult;
        readonly double[] DelayedFraction;

        readonly double[] DecayHeatQuantity = new double[4];
        readonly double[] DecayHeatTickMult;
        readonly double[] DecayHeatFraction;

        readonly double promptFraction;

        internal double BasePower = 60 * minPower;
        internal double Power = 8 * minPower;

        internal double PowerPct()
        {
            return Power / 3926000000;
        }
        readonly double baseTemperature;

        bool scram = false;
        int scramDelay = 0;

        //The intended reactor is approx 386 m^3 of water, and 296 m^3 of steam
        internal double WaterKg = 1000 * 371;
        internal double FuelTemp;
        internal double WaterTemp;

        internal double SteamKg = 0;
        internal double SteamTemperature = 0;
        internal double Pressure = 0;
        internal double SteamVolume = 296;
        double lastSteamGenerated = 0;

        double previousReactorWaterLevel = 0;
        internal bool WaterLevelGoingOutOfRange = false;
        internal double WaterLevel = 13.5;
        internal double ControlRodPos = 1;
        internal double RecirculationValve = 0;

        internal double PreviousPressure;

        internal double pcm = 0;

        internal double delayedPowerFraction;

        internal double Period;
        internal double DoublingTime;
        internal double RecirculationKg;
        internal double FuelEnergy;
        internal double VoidFraction;

        internal double DecayHeat;

        double previousPCM;
        internal double periodScramGrace = 0;

        internal double VesselTemp; //Shell then should be 1000 tons* 1000 kg/ton* 1000 g/kg* .5 = 500MJ per degree

        const int voidHistoryLength = 8;
        internal double[] VoidFractionHistory = new double[voidHistoryLength];

        internal void Process()
        {
            PreviousPressure = Pressure;
            previousPCM = pcm;
            pcm = 0;
            if (Form.checkBoxPCMOverride.Checked)
            {
                pcm = (double)Form.PCM;
            }
            else
            {
                if (scram)
                {
                    scramDelay++;
                    if (scramDelay > 2)
                    {
                        ControlRodPos += .04;
                        if (ControlRodPos >= 1)
                        {
                            ControlRodPos = 1;
                            scram = false;
                        }
                    }

                }
                else
                {
                    if (Form.radioButtonRods.Checked)
                    {
                        double speed = .0001;
                        if (Form.listBox1.SelectedIndex == 2)
                            speed = .0002;
                        else if (Form.listBox1.SelectedIndex == 0)
                            speed = 0.000025;
                        double setPoint = Form.Rods / 100;
                        ControlRodPos = MoveTowards(ControlRodPos, setPoint, speed);
                    }
                    else //if (Form.radioButtonMWTarget.Checked)
                    {
                        double setPower = 0;
                        if (double.TryParse(Form.textBoxMWTarget.Text, out double result))
                            setPower = result * Form.MWMultiplier;
                        double setPoint;
                        double speed = 0.0001;
                        double pctOff = Power / (setPower * 1000000);
                        if (pctOff < 1)
                            pctOff = 1 / pctOff;
                        double maxPcm = 170;
                        if (Power > setPower * 1000000)
                        {
                            setPoint = 1;
                        }
                        else
                        {
                            if (previousPCM > maxPcm)
                                setPoint = 1;
                            else if (previousPCM < maxPcm * .98)
                                setPoint = 0;
                            else
                                setPoint = ControlRodPos;
                            if (previousPCM > 100)
                                speed /= Math.Pow(previousPCM / 100, 4);
                        }

                        if (pctOff < 1.1)
                            speed /= 2;

                        if (pctOff < 1.015)
                            speed /= 2;

                        if (pctOff < 1.005)
                            speed /= 2;

                        if (pctOff < 1.0005)
                            speed /= 5;

                        ControlRodPos = MoveTowards(ControlRodPos, setPoint, speed);
                    }
                }

                pcm = 12740 - ControlRodPos * 30940;
            }

            if (WaterTemp > 90 && Form.Recirculation < .3)
            {
                Form.SetRecirculation(0.3, true);
                sim.MakeReport("Water temperature above 90 C without baseline recirculation, increasing pumps to 30%");
            }

            if (Form.checkBoxRecircAuto.Checked)
            {
                if (Form.radioButtonMWTarget.Checked)
                {  //This is slightly tricky, because modeling it on the current power just causes it to do wild power swings
                    var pct = Convert.ToDouble(Form.textBoxMWTarget.Text) * Form.MWMultiplier / 3926;
                    if (pct < .7)
                        Form.SetRecirculation(Lerp(.3, .5, pct / .7), false);
                    else if (pct < 1)
                        Form.SetRecirculation(Lerp(.5, 1, (pct - .7) / .3), false);
                    else
                        Form.SetRecirculation(1, false);
                }
                else
                {
                    Form.checkBoxRecircAuto.Checked = false;
                }


            }
            RecirculationValve = MoveTowards(RecirculationValve, (double)Form.Recirculation, .01);
            RecirculationKg = Lerp(RecirculationKg, RecirculationValve * 14530, .01);
            sim.totalPumpPowerUsage = 800000 * RecirculationValve;
            WaterTemp += 800000 * RecirculationValve / 4200 / WaterKg;
            if (lastSteamGenerated == 0 || WaterTemp < 102) //The watertemp is a little bit of a dirty hack to stop the initial boiling from causing a scram
                VoidFraction = 0;
            else
                VoidFraction = lastSteamGenerated * 10 / RecirculationKg;

            double voidFractionTotal = VoidFraction + VoidFractionHistory[voidHistoryLength - 1];
            for (int i = voidHistoryLength - 2; i >= 0; i--)
            {
                voidFractionTotal += VoidFractionHistory[i];
                VoidFractionHistory[i + 1] = VoidFractionHistory[i];
            }
            VoidFractionHistory[0] = VoidFraction;

            VoidFraction = voidFractionTotal / (voidHistoryLength + 1);


            if (Form.checkBoxRHR.Checked)
            {
				//Could potentially rework this to 'Flow rate at 275 kPa - 954 m^3 / hr'
                double rhrPower = 3400000 * (WaterTemp - baseTemperature) / (100 - baseTemperature); //34 MW
                WaterTemp -= rhrPower / 4200 / WaterKg;
            }


            //pcm -= 100 * (-0.000009765 * FuelTemp * FuelTemp + 0.01744 * FuelTemp + 0.04771);
            //if (WaterTemp <= 100)
            //    pcm -= 100 * (.0000409 * WaterTemp * WaterTemp + .01217 * WaterTemp);
            //else
            //    pcm -= 100 * (.00000010645 * Math.Pow(WaterTemp, 4) - .0000853 * Math.Pow(WaterTemp, 3) + .02402 * Math.Pow(WaterTemp, 2) - 2.553 * WaterTemp + 91.381);
            //double tempVF = VoidFraction * 100;
            //pcm -= 100 * (.0002701 * Math.Pow(tempVF, 4) - .01002 * Math.Pow(tempVF, 3) + .05033 * Math.Pow(tempVF, 2) + 2.662 * tempVF);

            //Based on an MIT lecture
            pcm -= 1.7 * (FuelTemp - 20);
            pcm -= 17 * (WaterTemp - 20);
            pcm -= 14400 * VoidFraction; //On a scale from 0 to 1
            //Control rod pcm seems to be ballpark - 18200 to 12740

            double delayedPower = 0;
            for (int i = 0; i < 6; i++)
            {
                delayedPower += DelayedQuantity[i] * (1 - DelayedTickMult[i]);
                DelayedQuantity[i] *= DelayedTickMult[i];
            }



            double kinf = 1 + pcm / 100000;
            Period = .085 / (kinf - 1);



            double PowerGain;
            double prevPower = Power - DecayHeat;
            BasePower *= promptFraction;

            if (pcm < 650)
                Power = BasePower * 650 / (650 - pcm);
            else
            {
                Power = BasePower * 650;
                sim.MakeReport("Prompt critical!  This would result in severe damage to the fuel or possibly the entire reactor");
                sim.Interrupt = true;
                Form.checkBoxPause.Checked = true;
            }

            double modPeriod = Period / Math.Pow(pcm, pcm / 3500); //Used to approximate the extra boost in the research reactor

            if (Period >= 0)
                PowerGain = (Power * Math.Pow(Math.E, 1 / modPeriod * 0.1)) - Power;
            else
                PowerGain = -((Power * Math.Pow(Math.E, 1 / -Period * 0.1)) - Power);


            BasePower += PowerGain + delayedPower + minPower;


            if (pcm < 650) //Put here as well to ensure that the ending power matches the beginning power next cycle, and to make the Period calc more accurate
                Power = BasePower * 650 / (650 - pcm);
            else
                Power = BasePower * 650;

            delayedPowerFraction = delayedPower / Power / .0065;

            for (int i = 0; i < 6; i++)
            {
                DelayedQuantity[i] += DelayedFraction[i] * Power;
            }

           

            DoublingTime = Math.Log(2) / Math.Log(Power / prevPower) / 10;

            Period = DoublingTime / Math.Log(2);
            if (Math.Abs(Period) > 1000)
                Period = double.PositiveInfinity;

            if (Period > 0 && Period < 10 && Form.checkBoxPCMOverride.Checked == false && Form.checkBoxProtection.Checked)
            {
                periodScramGrace++;
                if (periodScramGrace > 4)
                {
                    Scram("Reactor SCRAM - reactor period dangerously low (less than 10 seconds)");
                }
            }
            else
                periodScramGrace = 0;

            DecayHeat = 0;
            for (int i = 0; i < 4; i++)
            {
                DecayHeat += DecayHeatQuantity[i] * (1 - DecayHeatTickMult[i]);
                DecayHeatQuantity[i] *= DecayHeatTickMult[i];
                DecayHeatQuantity[i] += DecayHeatFraction[i] * Power;
            }

            Power += DecayHeat; //Deliberately outside of the prompt and period code

            if (PowerPct() > 1.18 && Form.checkBoxProtection.Checked)
            {
                Scram("Reactor SCRAM - reactor power exceeding 118%");
            }

            FuelTemp += Power / 120 / 159000 / 10;
            sim.EnergyTracker.FuelGeneratedEnergy = Power / 10;
            double deltaT = FuelTemp - WaterTemp;
            int tempRate = 1220000; //Should eventually change based on voids and recirc?
            FuelTemp -= tempRate * deltaT / 120 / 159000;
            FuelEnergy = FuelTemp * 120 * 159000;
            sim.EnergyTracker.FuelToWaterEnergy = tempRate * deltaT;
            WaterTemp += tempRate * deltaT / 4200 / WaterKg;

            var vesselDiff = WaterTemp - VesselTemp;
            WaterTemp -= vesselDiff * 0.0001;
            var energyMoved = WaterKg * vesselDiff * 0.001 * 4180;
            VesselTemp += energyMoved / 500000000;
            sim.EnergyTracker.CoolantToVesselEnergy = energyMoved;

            double boilingTemperature;
            if (Pressure < 95)
                boilingTemperature = 100;
            else
            {
                boilingTemperature = StmInt.Tsat(Pressure);
                if (boilingTemperature < 100) boilingTemperature = 100;
            }



            double boiledEnergy = 0;
            lastSteamGenerated = 0;

            if (WaterTemp > boilingTemperature)
            {
                if (SteamKg < 0.01)
                    SteamTemperature = boilingTemperature;
                else
                {
                    double tempDiff = boilingTemperature - SteamTemperature;
                    WaterTemp -= 0.5 * tempDiff * (SteamKg / (SteamKg + WaterKg));
                    boiledEnergy = 0.5 * tempDiff * (SteamKg / (SteamKg + WaterKg)) * 4200; //Not sure this is 100% right, but should be close
                    SteamTemperature += 0.5 * tempDiff * (WaterKg / (SteamKg + WaterKg));
                }

                for (int i = 0; i < 50; i++)
                {
                    if (boilingTemperature < 100) boilingTemperature = 100;
                    if (WaterTemp < boilingTemperature)
                        break;
                    //var waterSpecificHeat = stmProp.cppt(reactorPressure, boilingTemperature, ref stat, 0);
                    var excessEnergy = (WaterTemp - boilingTemperature) * 4200 * WaterKg / 1000 / 100;
                    var energyPerKg = StmInt.hgp(Pressure) - StmInt.hfp(Pressure);
                    var massChange = excessEnergy / energyPerKg;
                    double waterEnergy = WaterTemp * 4.200 * massChange;
                    double correctedEnergy = massChange * StmInt.hgp(Pressure) - waterEnergy;
                    SteamTemperature = Sim.GetMergedTemperature(massChange, SteamKg, boilingTemperature, SteamTemperature);
                    SteamKg += massChange;
                    lastSteamGenerated += massChange;
                    WaterKg -= massChange;
                    boiledEnergy += massChange * StmInt.hgp(Pressure) * 1000;
                    WaterTemp -= correctedEnergy / 4.200 / WaterKg;
                    Pressure = SteamKg / SteamVolume * Sim.steamPressureFactor;
                    boilingTemperature = StmInt.Tsat(Pressure);
                }
            }

            if (boilingTemperature < 100)
                boilingTemperature = 100;
            if (SteamKg > 0 && WaterTemp < boilingTemperature - .2)
            { //Not a perfect method of reactor steam condensing, but it would rarely happen.  
                var excessEnergy = 10000 * (boilingTemperature - .2 - WaterTemp) + 100;
                var energyPerKg = StmInt.hgp(Pressure) - StmInt.hfp(Pressure);
                var massChange = excessEnergy / energyPerKg;
                if (massChange > SteamKg)
                    massChange = SteamKg;
                SteamKg -= massChange;
                WaterKg += massChange;
                WaterTemp += energyPerKg * massChange / 4.200 / WaterKg;
            }



            sim.EnergyTracker.CoolantToSteamEnergy = boiledEnergy;

            if (Form.checkBoxRCIC.Checked)
            {
                if (Form.MSIV > 0)
                {
                    Form.SetMSIV(0, true);
                    sim.MakeReport("MSIV valve forced closed when RCIC is active");
                }


                var flow = Pressure / 100;
                SteamKg -= flow;

                Pressure = SteamKg / SteamVolume * Sim.steamPressureFactor;

                if (WaterLevel < 13)
                {
                    double amt = Math.Min(220 * (13 - WaterLevel), 250);
                    WaterKg += amt;
                    WaterTemp = (WaterKg * WaterTemp + amt * baseTemperature) / WaterKg;
                }

            }
        }

        internal void EndCycle()
        {
            if (WaterLevel > 13.5 && WaterLevel > previousReactorWaterLevel)
                WaterLevelGoingOutOfRange = true;
            else if (WaterLevel < 13.5 && WaterLevel < previousReactorWaterLevel)
                WaterLevelGoingOutOfRange = true;
            else
                WaterLevelGoingOutOfRange = false;
            previousReactorWaterLevel = WaterLevel;
        }

        internal void Scram(string message, bool forced = false)
        {
            if (Form.checkBoxProtection.Checked == false && forced == false)
                return;
            if (scram || ControlRodPos == 1)
                return;
            sim.MakeReport(message);
            scram = true;
            scramDelay = 0;
            Form.SetRods(100, true);
            Form.radioButtonRods.Checked = true;
        }

        internal void SetHigh()
        {
            RecirculationKg = 14530;
            lastSteamGenerated = 211.6;
            for (int i = 0; i < voidHistoryLength; i++)
            {
                VoidFractionHistory[i] = 0.1457;
            }

            SetDelayedNeutronsToCurrentPower();

            SteamTemperature = 287;

        }

        internal void SetDelayedNeutronsToCurrentPower()
        {
            for (int i = 0; i < 6; i++)
            {
                DelayedQuantity[i] = Power * DelayedFraction[i] / (1 - DelayedTickMult[i]);
            }
        }

        internal void SetDecayHeat(double power)
        {
            for (int i = 0; i < 4; i++)
            {
                DecayHeatQuantity[i] = power * (1 - .065) * DecayHeatFraction[i] / (1 - DecayHeatTickMult[i]);
            }
        }
    }
}
