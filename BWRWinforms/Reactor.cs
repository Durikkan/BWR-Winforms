using SteamProperties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BWRWinforms
{
    internal class Reactor
    {
        readonly Form1 Form;

        public Reactor(Form1 form, double baseTemperature)
        {
            Form = form;
            this.baseTemperature = baseTemperature;
            FuelTemp = baseTemperature;
            WaterTemp = baseTemperature;
            VesselTemp = baseTemperature;

            DelayedFraction = new double[] { .000215, .001424, .001274, .002568, .000748, .000273 };
            DelayedTickMult = new double[] { .998758, .996942, .988829, 0.97031, 0.8921, 0.73981 };
            promptFraction = 1 - DelayedFraction[0] - DelayedFraction[1] - DelayedFraction[2] - DelayedFraction[3] - DelayedFraction[4] - DelayedFraction[5];
            //These are mathed out to be fairly close... assuming the source I was working from is correct
        }

        const double minPower = .001; //.001

        double[] DelayedQuantity = new double[6];
        double[] DelayedTickMult;
        double[] DelayedFraction;

        double promptFraction;

        internal double BasePower = 60 * minPower;
        internal double Power = 8 * minPower;

        internal double PowerPct()
        {
            return Power / 3926000000;
        }
        double baseTemperature;

        const double kelvin = 273.15;

        bool scram = false;
        int scramDelay = 0;

        //534 / 831 cubic meters, for 13.5m height (out of 21m)
        //Seems as though the reactor core is 77.5 m^3, but I'm not sure whether that's 100% occupied or not
        //Taken at face value, that means the intended reactor is 457 m^3 of water, and 297 m^3 of steam
        internal double WaterKg = 1000 * 440;
        internal double FuelTemp;
        internal double WaterTemp;

        internal double SteamKg = 0;
        internal double SteamTemperature = 0;
        internal double Pressure = 0;
        internal double SteamVolume = 297;
        double lastSteamGenerated = 0;
        int stat = 0;

        double previousReactorWaterLevel = 0;
        internal bool WaterLevelGoingOutOfRange = false;
        internal double WaterLevel = 13.5;
        internal double ControlRodPos = 1;
        internal double RecirculationValve = 0;

        internal double pcm = 0;

        static readonly StmProp stmProp = new StmProp();

        internal double Period;
        internal double DoublingTime;
        internal double RecirculationKg;
        internal double FuelEnergy;
        internal double VoidFraction;

        double previousPCM;
        internal double periodScramGrace = 0;

        internal double VesselTemp; //Shell then should be 1000 tons* 1000 kg/ton* 1000 g/kg* .5 = 500MJ per degree

        internal double[] VoidFractionHistory = new double[20];

        internal void Process()
        {
            previousPCM = pcm;
            pcm = 0;
            if (Form.radioButtonPCM.Checked)
            {
                pcm = (double)Form.numericUpDownPCM.Value;
            }
            else if (Form.radioButtonRods.Checked)
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
                    double speed = .0001;
                    if (Form.listBox1.SelectedIndex == 2)
                        speed = .0002;
                    else if (Form.listBox1.SelectedIndex == 0)
                        speed = 0.000025;
                    double setPoint = (double)Form.numericUpDownRods.Value / 100;
                    ControlRodPos = Form.MoveTowards(ControlRodPos, setPoint, speed);
                }

                pcm = ControlRodPos * -18200 + 12740;
            }
            else if (Form.radioButtonMWTarget.Checked)
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
                    double setPower = 0;
                    if (double.TryParse(Form.textBoxMWTarget.Text, out double result))
                        setPower = result * (double)Form.numericUpDownMWMultiplier.Value;
                    double setPoint;
                    double speed = 0.0001;
                    double pctOff = Power / (setPower * 1000000);
                    if (pctOff < 1)
                        pctOff = 1 / pctOff;
                    double maxPcm = 150;
                    if (pctOff < 1.2)
                    {
                        maxPcm = (pctOff - 1) * 750;
                    }
                    if (Power > setPower * 1000000)
                    {
                        setPoint = 1;
                    }
                    else
                    {
                        if (previousPCM > maxPcm)
                            setPoint = 1;
                        else if (previousPCM < maxPcm * .9)
                            setPoint = 0;
                        else
                            setPoint = ControlRodPos;
                        if (previousPCM > 100)
                            speed /= 2;
                        if (previousPCM > 120)
                            speed /= 2;
                    }

                    if (pctOff < 1.1)
                        speed /= 2;

                    if (pctOff < 1.015)
                        speed /= 2;

                    if (pctOff < 1.005)
                        speed /= 2;

                    if (pctOff < 1.0005)
                        speed /= 5;

                    ControlRodPos = Form.MoveTowards(ControlRodPos, setPoint, speed);
                }


                pcm = ControlRodPos * -18200 + 12740;
            }

            if (WaterTemp > 99 && Form.numericUpDownRecirculation.Value < (decimal).3)
            {
                Form.numericUpDownRecirculation.Value = (decimal)0.3;
                Form.MakeReport("Water temperature above 90 C without baseline recirculation, increasing pumps to 30%");
            }

            if (Form.checkBoxRecircAuto.Checked)
            {
                if (Form.radioButtonMWTarget.Checked)
                {  //This is slightly tricky, because modeling it on the current power just causes it to do wild power swings
                    var pct = Convert.ToDouble(Form.textBoxMWTarget.Text) * (double)Form.numericUpDownMWMultiplier.Value / 3926;
                    if (pct < .7)
                        Form.numericUpDownRecirculation.Value = (decimal)Form.Lerp(.3, .5, pct / .7);
                    else if (pct < 1)
                        Form.numericUpDownRecirculation.Value = (decimal)Form.Lerp(.5, 1, (pct - .7) / .3);
                    else
                        Form.numericUpDownRecirculation.Value = 1;
                }
                else
                {
                    Form.checkBoxRecircAuto.Checked = false;
                }


            }
            RecirculationValve = Form.MoveTowards(RecirculationValve, (double)Form.numericUpDownRecirculation.Value, .001);
            RecirculationKg = RecirculationValve * 14530;
            Form.totalPumpPowerUsage = 800000 * RecirculationValve;
            WaterTemp += 800000 * RecirculationValve / 4200 / WaterKg;
            if (lastSteamGenerated == 0)
                VoidFraction = 0;
            else
                VoidFraction = lastSteamGenerated * 10 / RecirculationKg;
            double voidFractionTotal = VoidFraction + VoidFractionHistory[19];
            for (int i = 18; i >= 0; i--)
            {
                voidFractionTotal += VoidFractionHistory[i];
                VoidFractionHistory[i + 1] = VoidFractionHistory[i];
            }
            VoidFractionHistory[0] = VoidFraction;

            VoidFraction = voidFractionTotal / 21;


            if (Form.checkBoxRHR.Checked)
            {
                double rhrPower = 25400000 * (WaterTemp - baseTemperature) / (100 - baseTemperature); //254 MW
                WaterTemp -= rhrPower / 4200 / WaterKg;
            }



            //Based on an MIT lecture
            pcm -= (1.7 * (FuelTemp - 20));
            pcm -= (17 * (WaterTemp - 20));
            pcm -= 14400 * VoidFraction; //On a scale from 0 to 1
                                         //Control rod pcm seems to be ballpark -18200 to 12740

            double delayedPower = 0;
            for (int i = 0; i < 6; i++)
            {
                delayedPower += DelayedQuantity[i] * (1 - DelayedTickMult[i]);
                DelayedQuantity[i] *= DelayedTickMult[i];
            }



            double kinf = 1 + pcm / 100000;
            Period = .085 / (kinf - 1);



            double PowerGain;
            double prevPower = Power;
            BasePower *= promptFraction;

            if (pcm < 650)
                Power = BasePower * 650 / (650 - pcm);
            else
            {
                Power = BasePower * 650;
                Form.MakeReport("Prompt critical!  This would result in severe damage to the fuel or possibly the entire reactor");
            }

            double modPeriod = Period / Math.Pow(pcm, pcm / 1400); //Used to approximate the extra boost in the research reactor

            if (Period >= 0)
                PowerGain = (Power * Math.Pow(Math.E, 1 / modPeriod * 0.1)) - Power;
            else
                PowerGain = -((Power * Math.Pow(Math.E, 1 / -Period * 0.1)) - Power);


            BasePower += PowerGain + delayedPower;
            Power += PowerGain + delayedPower;

            Power += minPower;
            BasePower += minPower;

            for (int i = 0; i < 6; i++)
            {
                DelayedQuantity[i] += DelayedFraction[i] * Power;
            }

            DoublingTime = Math.Log(2) / Math.Log(Power / prevPower) / 10;

            Period = DoublingTime / Math.Log(2);
            if (Math.Abs(Period) > 1000)
                Period = double.PositiveInfinity;

            if (Period > 0 && Period < 10 && Form.radioButtonPCM.Checked == false && Form.checkBoxProtection.Checked)
            {
                periodScramGrace++;
                if (periodScramGrace > 3)
                {
                    Scram("Reactor SCRAM - reactor period dangerously low (less than 10 seconds)");
                }
            }
            else
                periodScramGrace = 0;

            if (PowerPct() > 1.18 && Form.checkBoxProtection.Checked)
            {
                Scram("Reactor SCRAM - reactor power exceeding 118%");
            }

            FuelTemp += Power / 120 / 159000 / 10;
            Form1.EnergyTracker.FuelGeneratedEnergy = Power / 10;
            double deltaT = FuelTemp - WaterTemp;
            int tempRate = 1220000; //Should eventually change based on voids and recirc?
            FuelTemp -= tempRate * deltaT / 120 / 159000;
            FuelEnergy = FuelTemp * 120 * 159000;
            Form1.EnergyTracker.FuelToWaterEnergy = tempRate * deltaT;
            WaterTemp += tempRate * deltaT / 4200 / WaterKg;

            var vesselDiff = WaterTemp - VesselTemp;
            WaterTemp -= vesselDiff * 0.0001;
            var energyMoved = WaterKg * vesselDiff * 0.001 * 4180;
            VesselTemp += energyMoved / 500000000;
            Form1.EnergyTracker.CoolantToVesselEnergy = energyMoved;

            var boilingTemperature = stmProp.Tsat(Pressure, ref stat, 0) - kelvin;
            if (boilingTemperature < 100) boilingTemperature = 100;

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
                    var energyPerKg = stmProp.hgp(Pressure, ref stat, 0) - stmProp.hfp(Pressure, ref stat, 0);
                    var massChange = excessEnergy / energyPerKg;
                    double waterEnergy = WaterTemp * 4.200 * massChange;
                    double correctedEnergy = massChange * stmProp.hgp(Pressure, ref stat, 0) - waterEnergy;
                    SteamTemperature = Form1.GetMergedTemperature(massChange, SteamKg, boilingTemperature, SteamTemperature);
                    SteamKg += massChange;
                    lastSteamGenerated += massChange;
                    WaterKg -= massChange;
                    boiledEnergy += massChange * stmProp.hgp(Pressure, ref stat, 0) * 1000;
                    WaterTemp -= correctedEnergy / 4.200 / WaterKg;
                    Pressure = SteamKg / SteamVolume * Form1.steamPressureFactor;
                    boilingTemperature = stmProp.Tsat(Pressure, ref stat, 0) - kelvin;
                }
            }

            if (boilingTemperature < 100)
                boilingTemperature = 100;
            if (SteamKg > 0 && WaterTemp < boilingTemperature)
            { //Not a perfect method of reactor steam condensing, but it would rarely happen.  
                var excessEnergy = 10000 * (boilingTemperature - WaterTemp) + 100;
                var energyPerKg = stmProp.hgp(Pressure, ref stat, 0) - stmProp.hfp(Pressure, ref stat, 0);
                var massChange = excessEnergy / energyPerKg;
                if (massChange > SteamKg)
                    massChange = SteamKg;
                SteamKg -= massChange;
                WaterKg += massChange;
                WaterTemp += excessEnergy * massChange / 4.200 / WaterKg;
            }


            //This isn't perfect, but is highly accurate at 7,100kPa, and pretty good at 1 kPa (gives .5226 instead of .5147)
            //I could always manually calculate it later if needed
            //reactorPressure = reactorSteamKg / reactorSteamVolume * steamPressureFactor;
            Form1.EnergyTracker.CoolantToSteamEnergy = boiledEnergy;

            if (Form.checkBoxRCIC.Checked)
            {
                if (Form.numericUpDownMSIV.Value > 0)
                {
                    Form.numericUpDownMSIV.Value = 0;
                    Form.MakeReport("MSIV valve forced closed when RCIC is active");
                }
                

                var flow = Pressure / 100;
                SteamKg -= flow;

                Pressure = SteamKg / SteamVolume * Form1.steamPressureFactor;

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
            Form.MakeReport(message);
            scram = true;
            scramDelay = 0;
            Form.numericUpDownRods.Value = 100;
            Form.radioButtonRods.Checked = true;
        }

        internal void SetHigh()
        {
            lastSteamGenerated = 211.6;
            for (int i = 0; i < 20; i++)
            {
                VoidFractionHistory[i] = 0.1457;
            }
            //Approx values after running for a little while
            DelayedQuantity[0] = 679613640.97461927;
            DelayedQuantity[1] = 1828195970.0686927;
            DelayedQuantity[2] = 447741736.04014379;
            DelayedQuantity[3] = 339574497.81500715;
            DelayedQuantity[4] = 27216386.768653918;
            DelayedQuantity[5] = 4119289.7774962904;
            SteamTemperature = 287;

        }
    }
}
