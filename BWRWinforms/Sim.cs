using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BWRWinforms.Utility;

namespace BWRWinforms
{
    internal class Sim
    {

        public Sim(Form1 form)
        {
            this.form = form;
            Reactor = new Reactor(form, this, baseTemperature);
            EnergyTracker = new EnergyTracker();
        }

        readonly Form1 form;

        /*
        Task List        		  		        
	       
		Continue some sort on the auto systems.

        Things to do...
        Minimal Automatic circulation - might be tricky to get exact since it looks complicated, but can do a rough value
        New PCM seems good so far
          Now just rods (though it might be tricky to get good deep values)
          Xenon - done in a similiar way to decay heat, set stable iodine, set stable xenon, maybe advance 1 hour xenon
        Maybe an option to pick from the 3 different pcm sets?
        Set new points to make up for the fixed PCM values
		I'm not sure how much faith to assign to the BWR_v3 sim.
        Better controls for rods/valves?  Clicking one adds a control thing at the top with <<< << < 0 > >> >>>
        Auto could control a setpoint, that would enable the auto recirc to control accordingly

        Kind of want to do two zones, core and surroundings, this makes the temperature reaction faster, and also makes recirc have a more pronounced effect
		
		*Separate thermal power from neutron power
		*Auto after shutdown got to 10s
		*Using an updown number doesn't refresh until you click away - might only be an issue for rods? 
		*Either fix, or implement that thing that makes the background pink when it's holding

        Options?
          Text level, simple, moderate and verbose

        Maybe make some sub-screens, like for the energy tracker.  Unsure whether I could do a virtual window that you can move
        stuff inside of, or actual pop-up windows.  

        Tutorial?

        I think I do need the feedwater rolling average

		Then at the end, do a pass to see how much it is to the calibration points below
		

        Calibration points:
          HP inlet pressure/temperature 6.792/283.7 MPa/o
		  Steam flow rate at nominal conditions 2122 kg/s
          Goal Feedwater temp is 215.6C
          Should be 1.3 After friction / gen inefficiency / plant load
		Once that seems stable, then you can do isentropic efficiency of the turbine, but as it stands now it's short even without that.
        Should be like 90% turbine eff, 98? gen eff, so ideally it should be like 1.5 GW    		        
        Should reorganize a bit to make things take advantage of the isentropic efficiency - basically the pressure is set by the condenser pressure, and the temperature/quality depends on the efficiency.  				                      
        

        Then perhaps a pass to add some more detail
        Better auto recirc on run-up?  (Auto on rods is basically impossible)
        Maybe better detail on feedwater heaters?
        There's a few things I want to add momentum to, preheaters, possibly also the turbine.   Basically each becomes a rolling average, where the current amount is added and 20% is removed.
        Condenser subcooling should depend on how much overkill there is in the circulation
        Kind of want to incorporate the increasing specific heat of water, too, it only really matters past 170C
        Power Grid connection
        Warming up the steam pipes, and drain the liquid                
        Turbine efficiency could potentially use a rework to affect the final quality and energy at each stage        
        Anything else that could use more detail          

        I had the idea but it may be redundant at this point 
          Reactor boiling logic revamp, the pure enthalpy method if that's not overly complicated.  Would basically be 'the water has 45GJ of energy, set temperature and steam %'
		Xenon?  I might do a time compressed version
        After everything is decently fleshed out, possibly move to WPF to create some basic graphics and an actual decent UI
        Then possibly some gamey elements (varying demand, scenarios, etc.)
         
         
         */
        internal const double baseTemperature = 20;

        internal bool Interrupt = false;

        int gameTick = 0;

        int reversePowerFrames = 0;

        //Steam Pipes? heat up, steam drain looks like .385 m ^ 3 per meter for a 700mm pipe, and like 170kg / m or 230 depending on whether it's s40 or s80        
        //That means the 4 50m pipes should be 340 or 460 tons, depending on the s40 or s80
        double steamKgInPipes = 0;
        double steamPipeValve = 0;
        double steamPipePressure = 0;
        readonly double steamPipeVolume = 4 * 50 * .385; //A dummy 50m pipe for now, note that there are four of these, which I'm just unifying for now

        internal const double steamPressureFactor = 191.335; //This isn't perfect, but is highly accurate at 7,100kPa, and pretty good at 1 kPa (gives .5226 instead of .5147)

        double bypassValve = 0;
        double makeupValve = 0;
        double returnToCSTValve = 0;

        double reheatTemperature = 0;

        //double condenserVolume = 1000;
        readonly double condenserAirVolume = 500;
        double condenserNonCondensibleKg = 500 * 1.225;
        double condenserSteamKg = 0;
        double condenserPressure = 0;
        double condenserWaterKg = 200000; //Technical max would be around 378000, ideal is about 250000
        double condenserTemperature = baseTemperature;
        double condenserCoolingWaterTemperature = baseTemperature;
        double condenserTubeWaterTemperature = baseTemperature;
        double condenserFlowValve = 1;
        double condenserSteamAverageEnthalpy = 0;
        double totalSteamCondenserAddedEnergy = 0;
        double totalWaterCondenserAddedEnergy = 0;
        double totalfeedwaterTankAddedEnergy = 0;

        internal double totalPumpPowerUsage = 0;

        double turbineValve = 0;
        double turbineEnergy = 0;
        double turbineRPM = 0;
        double turbineMechLosses = 0;
        const double turbineI = 311828 * .9;  //This should give about 3.6 gigawatt seconds for an 1800 rpm 1385 MW turbine
        double HPTPower = 0;
        double LPTPower = 0;
        double generatedPower = 0;
        double generatedNetPower = 0;
        double totalGeneratedPower = 0;
        double totalNetGeneratedPower = 0;

        double currentPIDRPM = 0;

        double turbineVibration = 0;
        double turbineRotorTemperature = 20;
        double turbineCasingTemperature = 20;
        double turbineDifferentialExpansion = 0;

        double turbineRecentAveragePower = 0;

        double turbineSteamEfficiency = 0;
        readonly double syncedTurbineEnergy = 0.5 * turbineI * 188.5 * 188.5;

        internal PIDController Turbinepid = new PIDController(-.002, -.001, -.008);
        internal PIDController TurbineRPMpid = new PIDController(.004, .002, .008);

        double finalFeedwaterTemp = 0;
        readonly HeatExchanger hp1 = new HeatExchanger(false);
        readonly HeatExchanger hp2 = new HeatExchanger(false);
        readonly HeatExchanger rh1 = new HeatExchanger(false);
        readonly HeatExchanger rh2 = new HeatExchanger(false);

        readonly HeatExchanger lp1 = new HeatExchanger(true);
        readonly HeatExchanger lp2 = new HeatExchanger(true);
        readonly HeatExchanger lp3 = new HeatExchanger(true);

        const double targetfeedwaterTankLevel = 100000;
        double previousFeedwaterTankWaterLevel = 0;
        bool feedwaterTankGoingOutOfRange = false;
        double feedwaterTankWaterKg = targetfeedwaterTankLevel;
        double feedwaterTankTemperature = baseTemperature;

        bool synced = false;

        double condenserOutflowValve = 0;
        double feedwaterValve = 0;

        internal EnergyTracker EnergyTracker;


        readonly Reactor Reactor;

        internal string HETemp;


        internal void SimTick(bool report)
        {
            HETemp = "";
            gameTick += 1;

            Reactor.Process();

            double steamFlowRate = 0;
            steamPipeValve = MoveTowards(steamPipeValve, form.MSIV, .01);
            if (steamPipeValve > 0)
            {
                var kgDiff = (Reactor.SteamKg / Reactor.SteamVolume) - (steamKgInPipes / steamPipeVolume);
                var pct = steamPipeValve * 42;
                steamFlowRate = kgDiff * pct * 10;
                EnergyTracker.SteamToPipeEnergy = kgDiff * pct * StmInt.hgp(Reactor.Pressure) * 1000;
                Reactor.SteamKg -= kgDiff * pct;
                steamKgInPipes += kgDiff * pct;
            }
            Reactor.Pressure = Reactor.SteamKg / Reactor.SteamVolume * steamPressureFactor;
            steamPipePressure = steamKgInPipes / steamPipeVolume * steamPressureFactor;
            var steamPipeEnthalpy = StmInt.hgp(steamPipePressure);

            if (form.checkBoxTurbineAuto.Checked)
            {
                TurbineAuto();

            }
            else
            {
                turbineValve = MoveTowards(turbineValve, (double)form.TurbineValve, .005);
                currentPIDRPM = turbineRPM; //Keep this reset so it grabs from the right place on restart
            }

            HPTPower = 0;
            LPTPower = 0;
            if (turbineValve > 0)
            {
                var kgTransfer = steamKgInPipes / 14.3 * turbineValve;
                double remainingKg = kgTransfer;
                double temperature = StmInt.Tsat(steamPipePressure);
                double startingTemperature = temperature;
                double remainingPressure;
                double condenserEntranceEnthalpy = Lerp(StmInt.hgp(condenserPressure), StmInt.hfp(condenserPressure), .1);
                double lastEnthalpy = steamPipeEnthalpy;
                double removedSteam;
                double enthalpy;
                double hChange;
                EnergyTracker.PipeToTurbineEnergy = kgTransfer * steamPipeEnthalpy * 1000;

                HeaterStep(Lerp(condenserPressure, steamPipePressure, 0.43210), .82, .088, .075, hp1, true);
                HeaterStep(Lerp(condenserPressure, steamPipePressure, 0.19121), .823, .046, .115, hp2, true);

                remainingPressure = Lerp(condenserPressure, steamPipePressure, 0.11372);
                temperature *= .882;
                enthalpy = Lerp(StmInt.hgp(remainingPressure), StmInt.hfp(remainingPressure), .135);
                hChange = lastEnthalpy - enthalpy;
                HPTPower += 1000 * hChange * remainingKg * 10;


                if (form.checkBoxHeaters.Checked)
                {
                    removedSteam = .085 * remainingKg;
                    rh1.AddDiminished(removedSteam, temperature, enthalpy);
                    remainingKg -= removedSteam;
                }


                //Always knock the water out
                rh2.Add(remainingKg * .135, StmInt.Tsat(remainingPressure), lastEnthalpy);
                remainingKg *= .865;

                enthalpy = StmInt.hgp(remainingPressure);

                if (form.checkBoxMSR.Checked && Reactor.SteamTemperature > 120)
                {
                    double kgReheat = kgTransfer / 11; //Approx, which is good enough for now
                    if (steamPipePressure < 5000) //This is kind of a shortcut method to keep temeperature from going too high
                        kgReheat *= steamPipePressure / 5000;
                    double reheatEnthalpy = StmInt.hft(Reactor.SteamTemperature - 20);
                    double addedEnergy = kgReheat * (steamPipeEnthalpy - reheatEnthalpy);
                    EnergyTracker.PipeToMainSteamReheatEnergy = steamPipeEnthalpy * kgReheat * 1000;
                    EnergyTracker.MainSteamReheatApplied = addedEnergy * 1000;

                    temperature = StmInt.Tph(remainingPressure, enthalpy + addedEnergy / remainingKg);
                    steamKgInPipes -= kgReheat;
                    reheatTemperature = temperature;

                    AddWaterToFeedwaterTank(kgReheat, temperature);
                }

                //enthalpy = stmProp.hpt(remainingPressure, temperature + kelvin, ref stat, 0);

                remainingPressure = Lerp(condenserPressure, steamPipePressure, 0.11246);
                lastEnthalpy = StmInt.hpt(remainingPressure, temperature);
                if (lastEnthalpy < 1000) //Detector for liquid, i.e. non super heated
                    lastEnthalpy = StmInt.hgp(remainingPressure);

                HeaterStep(Lerp(condenserPressure, steamPipePressure, 0.03316), .533, .034, 0, lp1, false);
                HeaterStep(Lerp(condenserPressure, steamPipePressure, 0.01365), .731, .033, .036, lp2, false);
                HeaterStep(Lerp(condenserPressure, steamPipePressure, 0.00609), .782, .027, .069, lp3, false);
                // .036 .069 .102

                hChange = lastEnthalpy - condenserEntranceEnthalpy;
                LPTPower += 1000 * hChange * remainingKg * 10;


                //Efficiency - this isn't perfect, but should do decently for now.  Ideally efficiency would be reflected in the consumed energy the whole path.  
                double efficiency = 1.005 - 0.75 * Math.Pow(2.718, -5.4 * kgTransfer / 190);
                turbineSteamEfficiency = efficiency;
                double unusedPower = (LPTPower + HPTPower) / 10 * (1 - efficiency);
                HPTPower *= efficiency;
                LPTPower *= efficiency;

                turbineEnergy += (HPTPower + LPTPower) / 10;

                condenserEntranceEnthalpy += unusedPower / 1000 / remainingKg;

                AddSteamToCondenser(remainingKg, condenserEntranceEnthalpy);
                steamKgInPipes -= kgTransfer;

                turbineCasingTemperature = Lerp(turbineCasingTemperature, startingTemperature, 0.00005 * kgTransfer);
                turbineRotorTemperature = Lerp(turbineRotorTemperature, startingTemperature, 0.0001 * kgTransfer);
                turbineDifferentialExpansion = (turbineRotorTemperature - turbineCasingTemperature) / 30;

                double newAverage = Lerp(turbineRecentAveragePower, HPTPower + LPTPower, .01);
                double powerDelta = Math.Abs(newAverage - turbineRecentAveragePower);
                turbineRecentAveragePower = newAverage;

                turbineVibration += Math.Abs(turbineDifferentialExpansion / 1000);
                turbineVibration += (.4 + Math.Abs(turbineDifferentialExpansion * 2)) * powerDelta / 200000000;

                if (turbineVibration > 1)
                    TurbineTrip("Turbine Trip - due to high vibration");
                if (turbineDifferentialExpansion > 1 || turbineDifferentialExpansion < -1)
                    TurbineTrip("Turbine Trip - due to high differential expansion");

                void HeaterStep(double newPressure, double tempMult, double steamLossPct, double endQuality, HeatExchanger exch, bool HP)
                {
                    remainingPressure = newPressure;
                    temperature *= tempMult;
                    enthalpy = Lerp(StmInt.hgp(remainingPressure), StmInt.hfp(remainingPressure), endQuality);
                    hChange = lastEnthalpy - enthalpy;
                    if (HP)
                        HPTPower += 1000 * hChange * remainingKg * 10;
                    else
                        LPTPower += 1000 * hChange * remainingKg * 10;
                    if (form.checkBoxHeaters.Checked)
                    {
                        removedSteam = steamLossPct * remainingKg;
                        remainingKg -= exch.AddDiminished(removedSteam, temperature, enthalpy);
                    }
                    lastEnthalpy = enthalpy;
                }


            }
            else
            { //Update these even when turbine valve is closed
                turbineDifferentialExpansion = (turbineRotorTemperature - turbineCasingTemperature) / 30;
                turbineVibration += Math.Abs(turbineDifferentialExpansion / 1000);
            }

            turbineCasingTemperature = Lerp(turbineCasingTemperature, baseTemperature, 0.000004);
            turbineRotorTemperature = Lerp(turbineRotorTemperature, turbineCasingTemperature, 0.0006);

            turbineVibration *= .995;

            turbineMechLosses = Lerp(10000, 3000000, turbineRPM / 1800);
            turbineMechLosses += Math.Pow(turbineRPM / 1800, 2) * 8000000;
            turbineEnergy -= turbineMechLosses / 10;
            if (turbineEnergy < 0)
                turbineEnergy = 0;
            if (synced)
            {
                double netPower = turbineEnergy - syncedTurbineEnergy;
                generatedPower = netPower * 10;
                totalGeneratedPower += netPower;
                turbineEnergy = syncedTurbineEnergy;
                EnergyTracker.totalTurbine = totalGeneratedPower;
                if (netPower < 0)
                    reversePowerFrames++;
                else
                    reversePowerFrames = 0;
                if (reversePowerFrames > 100)
                    TurbineTrip("Turbine Trip - Turbine was in reverse power for more than 10 seconds");
            }
            else
            {
                double lastRPM = turbineRPM;
                if (form.checkBoxTurningGear.Checked)
                {
                    if (turbineRPM < 30)
                    {
                        turbineEnergy += 3700; //Possible spec was 37kw motor
                    }
                }
                turbineRPM = Math.Sqrt(2 * turbineEnergy / turbineI) * 30 / Math.PI;
                turbineVibration += Math.Pow(lastRPM - turbineRPM, 2) / 1000;
                if (turbineRPM > 1880)
                    TurbineTrip("Turbine Trip - due to overspeed (> 1880 rpm)");
            }

            if (turbineRPM < 5 && form.checkBoxTurningGear.Checked == false && turbineRotorTemperature > 50)
            {
                MakeReport("Turbine is above 50C and at very low rpm, turning gear engaged to keep it from deforming.");
                form.checkBoxTurningGear.Checked = true;
            }

            double bypassTarget = (double)form.BypassValve;
            if (form.checkBoxAutoBypass.Checked)
            {
                if (Reactor.Pressure > 7200)
                {
                    bypassTarget = Math.Min((Reactor.Pressure - 7200) / 200, 1);
                }
                else if (turbineValve > 0 && Reactor.WaterLevel > 14.1)
                {
                    bypassTarget = Math.Min((Reactor.WaterLevel - 14.1) * 10, 1);
                }
                else
                    bypassTarget = 0;
            }

            bypassValve = MoveTowards(bypassValve, bypassTarget, 0.01);

            if (bypassValve > 0)
            {
                //This won't really work for this bit, but leaving it for now
                var kgDiff = (steamKgInPipes / 4 / steamPipeVolume) - (condenserSteamKg / condenserAirVolume);
                var pct = bypassValve * 10;
                EnergyTracker.BypassEnergy = kgDiff * pct * steamPipeEnthalpy * 1000;
                AddSteamToCondenser(kgDiff * pct, steamPipeEnthalpy);
                steamKgInPipes -= kgDiff * pct;
            }

            if (form.checkBoxCAR.Checked)
            {
                condenserNonCondensibleKg = Lerp(condenserNonCondensibleKg, 0, .0003);
            }

            if (form.checkBoxSJAEs.Checked)
            {
                if (condenserSteamKg > 0.1)
                {
                    condenserNonCondensibleKg = Lerp(condenserNonCondensibleKg, 0, .0009);
                    condenserSteamKg -= .01;
                }
                else
                {   //Uses reserve steam system
                    condenserNonCondensibleKg = Lerp(condenserNonCondensibleKg, 0, .0007);
                }
            }
            condenserFlowValve = MoveTowards(condenserFlowValve, (double)form.CondenserCirculation, 0.01);
            totalPumpPowerUsage += 1000000 * condenserFlowValve;

            if (condenserSteamKg > 0)
            {
                //This will likely need another pass at some point to make sure the condensed steam can't be hotter than the steam itself (Though I'm not completely sure how the logic works there)
                var flowMass = 37.85 * 1000 / 10;
                var endingEnthalpy = StmInt.hft(condenserCoolingWaterTemperature);
                var heat = condenserSteamKg * (condenserSteamAverageEnthalpy - endingEnthalpy) * 1000;
                var waterTemp = ((condenserTubeWaterTemperature * 4200 * flowMass) + heat) / (flowMass * 4200);
                double div = 1;
                if (waterTemp < 4000) //My shortcut doesn't like low flow, so this fixes it
                {
                    if (waterTemp > 95)
                    {
                        div = waterTemp / 95;
                        waterTemp /= div;
                    }
                    condenserCoolingWaterTemperature = waterTemp;
                    condenserTubeWaterTemperature = Lerp(waterTemp, baseTemperature, condenserFlowValve);
                    EnergyTracker.CondenserCoolingEnergy = (waterTemp - condenserTubeWaterTemperature) * flowMass * 4200;
                }
                else
                {
                    div = 999;
                    waterTemp = baseTemperature;
                }

                var curWater = condenserWaterKg;
                double convert = condenserSteamKg / div;
                condenserWaterKg += convert;
                condenserSteamKg -= convert;

                if (condenserWaterKg > 0)
                    condenserTemperature = (curWater * condenserTemperature + convert * waterTemp) / condenserWaterKg;

                condenserPressure = StmInt.Psat(waterTemp);
                condenserPressure += condenserNonCondensibleKg / condenserAirVolume * 82.45;
                condenserPressure += condenserSteamKg / condenserAirVolume * steamPressureFactor;

                if (condenserPressure > 110)
                    condenserPressure = 110;



            }
            else
            {
                condenserCoolingWaterTemperature = Lerp(condenserTubeWaterTemperature, baseTemperature, condenserFlowValve);
                EnergyTracker.CondenserCoolingEnergy = (condenserTubeWaterTemperature - condenserCoolingWaterTemperature) * 37.85 * 1000 / 10 * 4200;
                condenserTubeWaterTemperature = condenserCoolingWaterTemperature;
                condenserPressure = condenserNonCondensibleKg / condenserAirVolume * 82.45;
                condenserPressure += condenserSteamKg / condenserAirVolume * steamPressureFactor;
            }




            makeupValve = MoveTowards(makeupValve, (double)form.MakeupWater, 0.01);

            if (makeupValve > 0)
            {
                AddWaterToCondenser(makeupValve * 40, baseTemperature);
            }

            returnToCSTValve = MoveTowards(returnToCSTValve, (double)form.ToCST, 0.01);

            if (returnToCSTValve > 0)
            {
                double kg = returnToCSTValve * 40;
                if (kg > condenserWaterKg)
                    kg = condenserWaterKg;
                condenserWaterKg -= kg;
            }

            if (condenserPressure > 107)
            {
                //Condenser seal break, though it should be pretty unlikely to happen
                MakeReport("Condenser seal break, pressure exceeded 107 kPa");
                condenserNonCondensibleKg = 500 * 1.225;
                condenserSteamKg = 0;
            }

            condenserNonCondensibleKg = Lerp(condenserNonCondensibleKg, condenserAirVolume * 1.225, .00001);

            if (condenserPressure < 16 && form.checkBoxCAR.Checked)
            {
                MakeReport("Condenser pressure below 16 kPa, CARs are disabled as they're no longer effective");
                form.checkBoxCAR.Checked = false;
            }
            if (condenserSteamKg > 0 && form.checkBoxCAR.Checked)
            {
                MakeReport("Reactor steam in the condenser, CARs are disabled to prevent radioactivity from potentially being ejected");
                form.checkBoxCAR.Checked = false;
            }

            if (form.checkBoxCondenserAuto.Checked)
            {
                var diff = Math.Abs((targetfeedwaterTankLevel - feedwaterTankWaterKg) / 10000);
                var change = Math.Min(diff * .02, .0075);
                if (feedwaterTankWaterKg > targetfeedwaterTankLevel)
                {
                    if (feedwaterTankGoingOutOfRange && diff > .1)
                        condenserOutflowValve = MoveTowards(condenserOutflowValve, 0, 0.015);
                    else if (feedwaterTankGoingOutOfRange)
                        condenserOutflowValve = MoveTowards(condenserOutflowValve, 0, change * 2);
                    else
                        condenserOutflowValve = MoveTowards(condenserOutflowValve, 0, change);
                }
                else
                {
                    if (feedwaterTankGoingOutOfRange && diff > .1)
                        condenserOutflowValve = MoveTowards(condenserOutflowValve, 1, 0.015);
                    else if (feedwaterTankGoingOutOfRange)
                        condenserOutflowValve = MoveTowards(condenserOutflowValve, 1, change * 2);
                    else
                        condenserOutflowValve = MoveTowards(condenserOutflowValve, 1, change);
                }

            }
            else
                condenserOutflowValve = MoveTowards(condenserOutflowValve, (double)form.CondenserPumps, .015);

            double condenserOutflowPumpingRate = 0;

            if (condenserOutflowValve > 0)
            {
                var val = condenserOutflowValve * 151.4;
                if (val > condenserWaterKg)
                    val = condenserWaterKg;
                double initial = val;
                var curWater = feedwaterTankWaterKg;
                condenserOutflowPumpingRate = val * 10;
                var tempWater = condenserTemperature;
                totalPumpPowerUsage += 380000 * 3 * condenserOutflowValve;
                tempWater += (380000 * 3 * condenserOutflowValve) / 4200 / val;
                tempWater = lp3.ExchangeHeatAndCombine(ref val, tempWater, this);
                tempWater = lp2.ExchangeHeatAndCombine(ref val, tempWater, this);
                tempWater = lp1.ExchangeHeatAndCombine(ref val, tempWater, this);
                EnergyTracker.CondenserOutflowEnergy = condenserTemperature * initial * 4200;
                EnergyTracker.CondenserOutflowHeatingEnergy = tempWater * val * 4200 - EnergyTracker.CondenserOutflowEnergy;
                condenserWaterKg -= initial;
                feedwaterTankWaterKg += val;
                feedwaterTankTemperature = (curWater * feedwaterTankTemperature + val * tempWater) / feedwaterTankWaterKg;
            }

            if (feedwaterTankWaterKg > 150000 && form.CondenserPumps > 0 && form.checkBoxProtection.Checked)
            {
                MakeReport("Condenser pump tripped, too much water in the feedwater suction");
                form.SetCondenserPumps(0, true);
                form.checkBoxCondenserAuto.Checked = false;
            }


            if (form.checkBoxFeedAuto.Checked)
            {
                AutoFeedwater();
            }
            else
                feedwaterValve = MoveTowards(feedwaterValve, (double)form.FeedwaterPumps, .015);

            double feedwaterPumpingRate = 0;

            if (feedwaterValve > 0)
            {
                var val = feedwaterValve * 350.1;
                if (val > feedwaterTankWaterKg)
                    val = feedwaterTankWaterKg;
                var curWater = Reactor.WaterKg;
                feedwaterPumpingRate = val * 10;
                var tempWater = feedwaterTankTemperature;
                totalPumpPowerUsage += 1200000 * 3 * feedwaterValve;
                tempWater += (1200000 * 3 * feedwaterValve) / 4200 / val;
                tempWater = rh2.ExchangeHeatWater(val, tempWater, this);
                tempWater = rh1.ExchangeHeat(val, tempWater, this);
                tempWater = hp2.ExchangeHeat(val, tempWater, this);
                tempWater = hp1.ExchangeHeat(val, tempWater, this);
                EnergyTracker.FeedWaterEnergy = (feedwaterTankTemperature) * val * 4200;
                EnergyTracker.FeedwaterHeatingEnergy = (tempWater - feedwaterTankTemperature) * val * 4200;
                feedwaterTankWaterKg -= val;
                Reactor.WaterKg += val;
                finalFeedwaterTemp = tempWater;
                Reactor.WaterTemp = (curWater * Reactor.WaterTemp + val * tempWater) / Reactor.WaterKg;
            }

            if (Reactor.WaterLevel > 15 && form.FeedwaterPumps > 0 && form.checkBoxProtection.Checked)
            {
                MakeReport("Feedwater pump tripped, reactor water level above 15m");
                form.SetFeedwaterPumps(0, true);
                form.checkBoxFeedAuto.Checked = false;
            }


            if (Reactor.Pressure > 7600)
            {
                Reactor.Scram("Overpressure SCRAM - reactor pressure exceeded 7600 kPa");
            }

            double ventedSteam = 0;
            if (Reactor.Pressure > 7600)
            {  //Emergency Venting
                ventedSteam = 0.01 * Reactor.SteamKg;
                Reactor.SteamKg *= .99;
            }

            if (condenserPressure > 26 && form.TurbineValve > 0)
            {
                TurbineTrip("Turbine Trip - Condenser pressure above 26 kPa, turbine tripping to protect itself from the backpressure");
            }

            if (Reactor.WaterLevel > 14.25 && form.TurbineValve > 0)
            {
                TurbineTrip("Turbine Trip - Reactor water level above 14.25m, the steam quality has fallen and is too wet");
            }

            if (condenserPressure > 59 && form.BypassValve > 0)
            {
                form.SetBypassValve(0, true);
                MakeReport("Condenser pressure above 59 kPa, bypass closing");
            }

            if (condenserPressure > 80 && form.MSIV > 0)
            {
                form.SetMSIV(0, true);
                MakeReport("Condenser pressure above 80 kPa, MSIV closing");
                if (steamPipeValve > 0 && steamPipePressure > 20)
                {
                    MakeReport("Also auto-activating RCIC because the steam pipe was in active use");
                    form.checkBoxRCIC.Checked = true;
                }
            }

            if (Reactor.WaterLevel < 12)
            {
                Reactor.Scram("Reactor SCRAM - water level below 12m");
            }

            if (Reactor.WaterLevel < 10 && form.checkBoxRCIC.Checked == false)
            {
                form.checkBoxRCIC.Checked = true;
                MakeReport("Reactor Level dangerously low, RCIC activated");
            }

            if (form.checkBoxWaterCleanup.Checked)
            {
                if (Reactor.WaterLevel > 13.55)
                {
                    double val = Math.Min((Reactor.WaterLevel - 13.55) * 20 * 4.2, 4.2);
                    Reactor.WaterKg -= val;
                    AddWaterToCondenser(val, Reactor.WaterTemp);
                }
            }

            Reactor.WaterLevel = Reactor.WaterKg * StmInt.vft(Reactor.WaterTemp) / 28.59846;
            Reactor.SteamVolume = (21 - Reactor.WaterLevel) * 39.4666666;

            EnergyTracker.SteamEnteringCondenserEnergy = totalSteamCondenserAddedEnergy;
            totalSteamCondenserAddedEnergy = 0;
            EnergyTracker.WaterEnteringCondenserEnergy = totalWaterCondenserAddedEnergy;
            totalWaterCondenserAddedEnergy = 0;

            EnergyTracker.OtherFlowIntoFeedWaterEnergy = totalfeedwaterTankAddedEnergy;
            totalfeedwaterTankAddedEnergy = 0;

            EnergyTracker.TurbineWaste = turbineMechLosses;
            EnergyTracker.TurbineOutput = HPTPower + LPTPower;

            EnergyTracker.pumpInput = totalPumpPowerUsage;

            generatedNetPower = generatedPower - totalPumpPowerUsage * 10 - 15000000;
            totalNetGeneratedPower += generatedNetPower / 10;

            if (report || Interrupt)
            {
                double waterEnergy = Reactor.WaterTemp * 4200 * Reactor.WaterKg;
                double steamEnergy = 1000 * Reactor.SteamKg * StmInt.hgp(Reactor.Pressure);
                double vesselEnergy = Reactor.VesselTemp * 500000000;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Power: {ToSI(Reactor.Power)}Wth \t {Math.Round(Reactor.PowerPct() * 100, 2)}%");
                sb.AppendLine($"Period: {Math.Round(Reactor.Period, 2)} sec");
                sb.AppendLine($"Doubling time: {Math.Round(Reactor.DoublingTime, 2)} sec");
                sb.AppendLine($"Core flow: {Math.Round(Reactor.RecirculationKg, 0)} kg/s");
                sb.AppendLine($"Voids: {Math.Round(100 * Reactor.VoidFraction, 3)} %");
                sb.AppendLine($"Water Level: {Math.Round(Reactor.WaterLevel, 2)} m ({Math.Round(Reactor.WaterLevel - 13.5, 2)})");
                sb.AppendLine($"Water Mass: {Math.Round(Reactor.WaterKg, 0)} kg");
                sb.AppendLine($"Fuel Temperature: {Math.Round(Reactor.FuelTemp, 2),-6} C       \tEnergy: {ToSI(Reactor.FuelEnergy)}J");
                sb.AppendLine($"Water Temperature: {Math.Round(Reactor.WaterTemp, 2),-6} C     \tEnergy: {ToSI(waterEnergy)}J");
                sb.AppendLine($"Steam Temperature: {Math.Round(Reactor.SteamTemperature, 2),-6} C       \tEnergy: {ToSI(steamEnergy, capToZero: true)}J");
                sb.AppendLine($"Vessel Temperature: {Math.Round(Reactor.VesselTemp, 2),-6} C       \tEnergy: {ToSI(vesselEnergy, capToZero: true)}J");
                sb.AppendLine($"Reactor Steam Kg: {Math.Round(Reactor.SteamKg, 0)} kg");
                sb.AppendLine($"Reactor Pressure: {Math.Round(Reactor.Pressure, 0)} kPa");
                if (ventedSteam > 0)
                    sb.AppendLine($"***Reactor Venting Steam: {Math.Round(ventedSteam * 10, 0)} kg/s");
                sb.AppendLine($"Current Delayed Neutron Power: {Math.Round(Reactor.delayedPowerFraction * 100, 2)}%");
                sb.AppendLine($"Decay Heat Power: {ToSI(Reactor.DecayHeat)}Wth");
                if (form.checkBoxPCMOverride.Checked == false)
                    sb.AppendLine($"Control Rod Position: {Math.Round(Reactor.ControlRodPos * 100, 3)}%");
                sb.AppendLine($"Effective pcm: {Math.Round(Reactor.pcm, 1)}");
                sb.AppendLine();
                sb.AppendLine($"Reactor Steam To pipes: {Math.Round(steamFlowRate, 0)} kg/s");
                sb.AppendLine($"Steam Pipes Pressure: {Math.Round(steamPipePressure, 0)} kPa");
                sb.AppendLine($"Steam Pipes Mass: {Math.Round(steamKgInPipes, 0)} kg");
                sb.AppendLine();
                sb.AppendLine($"MSR Temperature: {Math.Round(reheatTemperature, 2)} C");
                sb.AppendLine($"Turbine RPM: {Math.Round(turbineRPM, 1)}");
                sb.AppendLine($"Turbine Energy: {ToSI(turbineEnergy, capToZero: true)}J");
                sb.AppendLine($"Turbine Mech Losses: {ToSI(turbineMechLosses)}W");
                sb.AppendLine($"Turbine Steam Efficiency: {Math.Round(turbineSteamEfficiency * 100, 1)}%");
                sb.AppendLine($"Turbine Rotor / Casing Temperature: {Math.Round(turbineRotorTemperature, 1)} C {Math.Round(turbineCasingTemperature, 1)} C");
                sb.AppendLine($"Vibration / Differential Expansion: {Math.Round(turbineVibration, 3)} {Math.Round(turbineDifferentialExpansion, 3)} ");
                sb.AppendLine($"HPT Energy Rate: {ToSI(HPTPower, capToZero: true)}W");
                sb.AppendLine($"LPT Energy Rate: {ToSI(LPTPower, capToZero: true)}W");
                sb.AppendLine($"Current Power Production: {ToSI(generatedPower, capToZero: true)}W");
                sb.AppendLine($"Current Net Power Production: {ToSI(generatedNetPower, capToZero: true)}W");
                sb.AppendLine($"Total Power Produced: {ToSI(totalGeneratedPower, capToZero: true)}J");
                sb.AppendLine($"Total Net Power Produced: {ToSI(totalNetGeneratedPower, capToZero: true)}J");
                sb.AppendLine();
                sb.AppendLine($"Bypass Valve: {Math.Round(bypassValve, 2)}");
                sb.AppendLine($"Condenser Pressure: {Math.Round(condenserPressure, 1)} kPa");
                sb.AppendLine($"Condenser Noncondensible Kg: {Math.Round(condenserNonCondensibleKg, 2)} kg");
                sb.AppendLine($"Condenser Steam Kg: {Math.Round(condenserSteamKg, 0)} kg");
                sb.AppendLine($"Condenser Cooling Water Temperature: {Math.Round(condenserCoolingWaterTemperature, 2)} C");
                sb.AppendLine($"Condenser Water Kg: {Math.Round(condenserWaterKg, 0)} kg");
                sb.AppendLine($"Condenser Water Temperature: {Math.Round(condenserTemperature, 2)} C");
                sb.AppendLine($"Condenser Outflow Pumping Rate: {Math.Round(condenserOutflowPumpingRate, 0)} kg/s");
                sb.AppendLine();
                sb.AppendLine($"Feedwater Suction Water Kg: {Math.Round(feedwaterTankWaterKg, 0)} kg");
                sb.AppendLine($"Feedwater Suction Water Temperature: {Math.Round(feedwaterTankTemperature, 2)} C");
                sb.AppendLine($"Feedwater Pumping Rate: {Math.Round(feedwaterPumpingRate, 0)} kg/s");
                sb.AppendLine($"Feedwater Final Temperature: {Math.Round(finalFeedwaterTemp, 2)} C");

                sb.AppendLine($"Total Pump Power usage: {ToSI(totalPumpPowerUsage * 10, capToZero: true)}W");

                sb.AppendLine($"Feedwater Heater Temperatures: \n{HETemp} C");


                form.label7.Text = $"Elapsed Time : {gameTick / 10.0}s";

                form.label11.Text = Math.Round(100 * Reactor.ControlRodPos, 3).ToString();
                form.label12.Text = Math.Round(Reactor.RecirculationValve, 3).ToString();
                form.label13.Text = Math.Round(steamPipeValve, 3).ToString();
                form.label14.Text = Math.Round(bypassValve, 3).ToString();
                form.label15.Text = Math.Round(turbineValve, 3).ToString();
                form.label16.Text = Math.Round(condenserOutflowValve, 3).ToString();
                form.label17.Text = Math.Round(feedwaterValve, 3).ToString();
                form.label18.Text = Math.Round(condenserFlowValve, 3).ToString();
                form.label19.Text = Math.Round(makeupValve, 3).ToString();
                form.label20.Text = Math.Round(returnToCSTValve, 3).ToString();

                form.richTextBox1.Text = sb.ToString();
                form.richTextBox2.Text = EnergyTracker.GetReport();
            }


            DisplayWarnings();

            Reactor.EndCycle();



            if (feedwaterTankWaterKg > targetfeedwaterTankLevel && feedwaterTankWaterKg > previousFeedwaterTankWaterLevel)
                feedwaterTankGoingOutOfRange = true;
            else if (feedwaterTankWaterKg < targetfeedwaterTankLevel && feedwaterTankWaterKg < previousFeedwaterTankWaterLevel)
                feedwaterTankGoingOutOfRange = true;
            else
                feedwaterTankGoingOutOfRange = false;
            previousFeedwaterTankWaterLevel = feedwaterTankWaterKg;
        }


        /// <summary>
        /// A quick way to add steam and merge the enthalpy with any steam already in there.  
        /// </summary>
        /// <param name="kg"></param>
        /// <param name="enthalpy"></param>
        void AddSteamToCondenser(double kg, double enthalpy)
        {
            if (kg == 0)
                return;
            totalSteamCondenserAddedEnergy += kg * enthalpy * 1000;
            condenserSteamAverageEnthalpy = GetMergedTemperature(kg, condenserSteamKg, enthalpy, condenserSteamAverageEnthalpy);
            condenserSteamKg += kg;
        }

        internal void AddWaterToCondenser(double kg, double temperature)
        {
            condenserTemperature = GetMergedTemperature(kg, condenserWaterKg, temperature, condenserTemperature);
            condenserWaterKg += kg;
            totalWaterCondenserAddedEnergy += kg * 4200 * temperature;
        }

        internal void AddWaterToFeedwaterTank(double kg, double temperature)
        {
            feedwaterTankTemperature = GetMergedTemperature(kg, feedwaterTankWaterKg, temperature, feedwaterTankTemperature);
            feedwaterTankWaterKg += kg;
            totalfeedwaterTankAddedEnergy += kg * 4200 * temperature;
        }

        internal static double GetMergedTemperature(double firstMass, double secondMass, double firstTemperature, double SecondTemperature)
        {
            if (firstMass == 0 && secondMass == 0)
                throw new Exception();
            return (firstMass * firstTemperature + secondMass * SecondTemperature) / (firstMass + secondMass);
        }

        internal void MakeReport(string str)
        {
            form.richTextBox3.Text = $"{gameTick / 10.0}s - {str}\n{form.richTextBox3.Text}";
        }

       

        internal void TurbineTrip(string reason)
        {
            if (form.checkBoxProtection.Checked == false)
                return;
            if (synced == false && turbineValve == 0)
                return; //Assumed already tripped
            form.SetTurbineValve(0, true);
            form.checkBoxTurbineAuto.Checked = false;
            turbineValve = 0;
            MakeReport(reason);
            reversePowerFrames = 0;
            form.SetBypassValve(1, true);
            if (synced)
            {
                synced = false;
                generatedPower = 0;
            }
            if (Reactor.PowerPct() > .4)
            {
                Reactor.Scram("Reactor SCRAM - Reactor tripping with turbine due to reactor power above bypass safety limits");
            }
        }



        internal void Scram()
        {
            Reactor.Scram("Manual Reactor SCRAM", true);
        }

        internal void SyncTurbine()
        {
            if (synced)
            {
                MakeReport("Turbine manually desynced");
                synced = false;
                generatedPower = 0;
                reversePowerFrames = 0;
            }
            else if ((turbineRPM > 1798 && turbineRPM < 1802) || Control.ModifierKeys == Keys.Shift)
            {
                if (form.checkBoxTurbineAuto.Checked == false)
                    form.listBox2.SelectedIndex = 2;
                MakeReport("Turbine synced");
                synced = true;
                turbineRPM = 1800;
            }
        }

        internal void SetCondenserVacuum()
        {
            condenserNonCondensibleKg = 29;
        }



        internal void SetToFullLoad()
        {
            form.SetMSIV(1, true);
            form.SetTurbineValve(1, true);
            form.SetFeedwaterPumps(.60, true);
            form.SetCondenserPumps(.8, true);
            condenserOutflowValve = .8;
            feedwaterValve = .60;
            steamPipeValve = 1;
            turbineValve = 1;
            form.radioButtonMWTarget.Checked = true;
            form.checkBoxSJAEs.Checked = true;
            condenserNonCondensibleKg = 7;
            Reactor.WaterKg = 283000;
            Reactor.WaterTemp = 287.7;
            Reactor.FuelTemp = 590;
            Reactor.ControlRodPos = .165;
            Reactor.Power = 3926000000;
            Reactor.BasePower = 3926000000;
            synced = true;
            turbineRPM = 1800;
            condenserWaterKg = 250000;
            form.checkBoxFeedAuto.Checked = true;
            form.checkBoxCondenserAuto.Checked = true;
            condenserTemperature = 40;
            feedwaterTankTemperature = 126;
            Reactor.SteamKg = 10870;
            Reactor.Pressure = 7240;
            steamKgInPipes = 2582;
            turbineCasingTemperature = 285;
            turbineRotorTemperature = 285;
            turbineRecentAveragePower = 1300000000;
            form.SetRecirculation(1, true);
            Reactor.RecirculationValve = 1;
            form.SetMWMultiplier(1, true);
            Reactor.VesselTemp = 288;
            Reactor.SetHigh();
            form.listBox2.SelectedIndex = 2;

            form.checkBoxProtection.Checked = false;
            //Rather than set it exactly as even a small error makes it fluctuate severely, just let it sim a bit to reach rough equalibrium
            //AdvanceFrames(100);
            EnergyTracker = new EnergyTracker();
            totalGeneratedPower = 0;
            totalNetGeneratedPower = 0;
            form.checkBoxProtection.Checked = true;

        }

        internal void ResetTrackedEnergy()
        {
            EnergyTracker = new EnergyTracker();
            totalGeneratedPower = 0;
            totalNetGeneratedPower = 0;
        }

        internal void BoilStart()
        {
            form.radioButtonMWTarget.Checked = true;
            Reactor.WaterTemp = 100;
            Reactor.FuelTemp = 100;
            Reactor.ControlRodPos = .363;
            Reactor.Power = 392600000;
            Reactor.SetDelayedNeutronsToCurrentPower();
            form.SetMWMultiplier(.1, true);
            Reactor.BasePower = 392600000;
            Reactor.RecirculationValve = .3;
            form.SetRecirculation(.3, true);
            Reactor.VesselTemp = 95;
            form.checkBoxSJAEs.Checked = true;
            condenserNonCondensibleKg = 7;
        }

        internal void AdvanceFrames(int frames)
        {
            Interrupt = false;
            for (int i = 0; i < frames - 1; i++)
            {
                SimTick(false);
                if (Interrupt)
                    return;
            }
            if (Interrupt)
                return;
            SimTick(true);
        }


        private void DisplayWarnings()
        {
            StringBuilder sb = new StringBuilder();

            if (Reactor.Pressure > 7300)
                sb.AppendLine("Reactor Pressure High");

            if (Reactor.WaterLevel > 14.75)
                sb.AppendLine("Reactor Level High High");
            else if (Reactor.WaterLevel > 14)
                sb.AppendLine("Reactor Level High");

            if (Reactor.PowerPct() > 1.04)
                sb.AppendLine("Reactor Power High");

            if (Reactor.WaterLevel < 13)
            {
                sb.AppendLine("Reactor Level Low");
            }


            if (Reactor.Period > 0 && Reactor.Period < 10)
                sb.AppendLine("Reactor period extremely low, SCRAM imminent");
            else if (Reactor.Period > 0 && Reactor.Period < 20)
                sb.AppendLine("Reactor period low");

            if (Reactor.PowerPct() > .25 && Reactor.RecirculationValve < .4)
                sb.AppendLine("Recirculation too low");

            if (Reactor.PowerPct() - .71 - (Reactor.RecirculationValve - .4) / 2 > 0)
                sb.AppendLine("Power too high for current recirculation!");

            if (Reactor.RecirculationValve - Reactor.PowerPct() > .55)
                sb.AppendLine("Recirculation too high, exceeding steam separator limit");

            if (turbineRPM > 1820)
                sb.AppendLine("Turbine High RPM");

            if (reversePowerFrames > 2)
                sb.AppendLine("Turbine Reverse Power");

            if (turbineVibration > .8)
                sb.AppendLine("Turbine Vibration High");
            if (turbineDifferentialExpansion > .8)
                sb.AppendLine("Turbine Differential Expansion High");
            if (turbineDifferentialExpansion < -.8)
                sb.AppendLine("Turbine Differential Expansion High (in the negative direction)");

            if (feedwaterTankWaterKg > 130000)
                sb.AppendLine("Feedwater Suction Water Level High");
            if (feedwaterTankWaterKg < 50000)
                sb.AppendLine("Feedwater Suction Water Level Low");

            if (condenserPressure > 80)
                sb.AppendLine("No Condenser Vaccum");
            else if (condenserPressure > 19)
                sb.AppendLine("Condenser Pressure High");

            if (condenserWaterKg > 300000)
                sb.AppendLine("Condenser Hotwell Level High");
            if (condenserWaterKg < 150000)
                sb.AppendLine("Condenser Hotwell Level Low");


            form.richTextBox4.Text = sb.ToString();

        }

        internal void ListBox2_SelectedIndexChanged()
        {
            TurbineRPMpid.ResetIntegral(0);
            if (form.listBox2.SelectedIndex != 2)
            {
                currentPIDRPM = turbineRPM;
            }

        }
        private void AutoFeedwater()
        { //This is down here because it's bulky
            var diff = Math.Abs(13.5 - Reactor.WaterLevel);
            var change = Math.Min(diff * .04, .0075);
            if (diff < .01)
            {
                bool under = 13.5 - Reactor.WaterLevel > 0;
                int target = under ? 1 : 0;
                double smallRate = .0005;
                if (diff < .001)
                    smallRate /= 4;
                if (diff < .0001)
                    smallRate /= 4;

                feedwaterValve = MoveTowards(feedwaterValve, target, 0.00005);

                if (Reactor.WaterLevelGoingOutOfRange)
                {
                    feedwaterValve = MoveTowards(feedwaterValve, target, smallRate);
                }
                else
                {
                    smallRate /= 4;
                    feedwaterValve = MoveTowards(feedwaterValve, target, smallRate);
                }

            }
            else if (Reactor.WaterLevel > 13.5)
            {
                if (diff < .1)
                {
                    if (Reactor.WaterLevelGoingOutOfRange)
                        feedwaterValve = MoveTowards(feedwaterValve, 0, 0.0005);
                    else
                        feedwaterValve = MoveTowards(feedwaterValve, 1, 0.0005);
                }

                if (Reactor.WaterLevelGoingOutOfRange && diff > .01)
                    feedwaterValve = MoveTowards(feedwaterValve, 0, 0.015);
                else if (Reactor.WaterLevelGoingOutOfRange)
                    feedwaterValve = MoveTowards(feedwaterValve, 0, change * 2);
                else
                    feedwaterValve = MoveTowards(feedwaterValve, 0, change);
            }
            else if (finalFeedwaterTemp < 200)
            {
                change /= 6;
                if (Reactor.WaterLevelGoingOutOfRange)
                    feedwaterValve = MoveTowards(feedwaterValve, 1, change);
                else
                    feedwaterValve = MoveTowards(feedwaterValve, 1, change / 4);
            }
            else
            {
                if (diff < .1)
                {
                    if (Reactor.WaterLevelGoingOutOfRange)
                        feedwaterValve = MoveTowards(feedwaterValve, 1, 0.001);
                    else
                        feedwaterValve = MoveTowards(feedwaterValve, 0, 0.001);
                }
                if (Reactor.WaterLevelGoingOutOfRange && diff > .01)
                    feedwaterValve = MoveTowards(feedwaterValve, 1, 0.015);
                else if (Reactor.WaterLevelGoingOutOfRange)
                    feedwaterValve = MoveTowards(feedwaterValve, 1, change * 2);
                else
                    feedwaterValve = MoveTowards(feedwaterValve, 1, change);
            }
        }
        private void TurbineAuto()
        {
            if (form.listBox2.SelectedIndex == 2)
            {
                if (synced == false)
                {
                    MakeReport("Auto is set for a pressure and turbine is not synched, disengaging auto");
                    form.checkBoxTurbineAuto.Checked = false;
                    return;
                }
                double distance = Math.Abs(7170 - Reactor.Pressure);
                double rate = Reactor.Pressure - Reactor.PreviousPressure;
                double speed = .0025;
                if (distance < 10)
                {
                    if (rate < 0)
                        turbineValve = MoveTowards(turbineValve, 0, .0005);
                    if (rate > 0)
                        turbineValve = MoveTowards(turbineValve, 1, .0005);
                    speed /= 2;
                }
                if (Reactor.Pressure > 7170)
                {
                    if (rate > 0)
                        turbineValve = MoveTowards(turbineValve, 1, speed);
                    else
                        turbineValve = MoveTowards(turbineValve, 1, .0001);
                }
                else if (generatedPower - turbineMechLosses > 20000000) //Don't reduce power if it makes it go too close to reverse power
                {
                    if (rate < 0)
                        turbineValve = MoveTowards(turbineValve, 0, speed);
                    else
                    {
                        if (distance > 30 * rate)
                            turbineValve = MoveTowards(turbineValve, 0, speed / 2);
                        if (distance < 10 * rate)
                            turbineValve = MoveTowards(turbineValve, 1, .0001);
                        else
                            turbineValve = MoveTowards(turbineValve, 0, .0001);
                    }
                }

            }
            else
            {
                if (synced)
                {
                    MakeReport("Auto is set for a specific rpm and turbine is synched, disengaging auto");
                    form.checkBoxTurbineAuto.Checked = false;
                    return;
                }
                double goal = 1800;
                if (form.listBox2.SelectedIndex == 0)
                    goal = 500;
                double goalSpeed = 0.5;
                if (turbineDifferentialExpansion > .6)
                {
                    goalSpeed *= 3.333 * (.9 - turbineDifferentialExpansion);
                }
                if (turbineDifferentialExpansion < .9)
                    currentPIDRPM = MoveTowards(currentPIDRPM, goal, goalSpeed);
                double output = TurbineRPMpid.CalculateOutput(turbineRPM, currentPIDRPM);
                double speed = .0025;
                double diff = Math.Abs(output - turbineValve);
                if (diff < .001)
                    diff = .001;
                if (diff < .05)
                    speed = speed / .05 * diff;
                turbineValve = MoveTowards(turbineValve, output, speed);
            }
        }

        internal void SetDecayHeat()
        {
            Reactor.SetDecayHeat(Reactor.Power);
        }
    }
}

