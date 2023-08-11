using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BWRWinforms
{
    internal class HeatExchanger
    {

        double kg;
        double temperature;
        double enthalpy;
        readonly bool condenserDump;

        public HeatExchanger(bool condenserDump)
        {
            this.condenserDump = condenserDump;
        }

        internal double AddDiminished(double kg, double temperature, double enthalpy)
        {
            if (kg == 0)
                return 0;

            if (kg + this.kg > 25)
            {
                kg = 25 - this.kg;
            }

            this.temperature = Sim.GetMergedTemperature(kg, this.kg, temperature, this.temperature);

            this.enthalpy = enthalpy;

            this.kg += kg;     

            return kg;
        }

        internal void Add(double kg, double temperature, double enthalpy)
        {
            if (kg == 0)
                return;
            this.temperature = Sim.GetMergedTemperature(kg, this.kg, temperature, this.temperature);

            this.enthalpy = enthalpy;

            this.kg += kg;
    
        }

        internal double ExchangeHeatWater(double waterMass, double waterTemperature, Sim sim)
        {
            var newTemp = Sim.GetMergedTemperature(kg, waterMass, temperature, waterTemperature);
            temperature = newTemp;
            if (condenserDump)
                //form.AddWaterToCondenser(kg, temperature);
                throw new Exception();
            else
                sim.AddWaterToFeedwaterTank(kg, temperature);
            kg = 0;
            sim.HETemp += $" *{Math.Round(newTemp, 1)} ";
            return newTemp;
        }

        internal double ExchangeHeat(double waterMass, double waterTemperature, Sim sim)
        {

            var simpleMergeTemp = Sim.GetMergedTemperature(12 * kg, waterMass, temperature, waterTemperature);
            double afterEnthalpy = StmInt.hft(simpleMergeTemp);

            double addedEnergy = kg * (enthalpy - afterEnthalpy);

            double attemptedTemperature = waterTemperature + (addedEnergy / 4.200 / waterMass);
            double fraction = 1;
            if (attemptedTemperature > temperature)
            {
                fraction = (temperature - waterTemperature) / (attemptedTemperature - waterTemperature);
                if (fraction < 0)
                {
                    sim.HETemp += $" {Math.Round(waterTemperature, 1)} ";
                    return waterTemperature;
                }
                else
                    attemptedTemperature = waterTemperature + fraction * (attemptedTemperature - waterTemperature);
            }
            if (condenserDump)
                sim.AddWaterToCondenser(kg * fraction, attemptedTemperature);
            else
                sim.AddWaterToFeedwaterTank(kg * fraction, attemptedTemperature);
            kg *= 1 - fraction;
            sim.HETemp += $" {Math.Round(attemptedTemperature, 1)} ";
            return attemptedTemperature;
        }

        internal double ExchangeHeatAndCombine(ref double waterMass, double waterTemperature, Sim sim)
        {

            var simpleMergeTemp = Sim.GetMergedTemperature(12 * kg, waterMass, temperature, waterTemperature);
            double afterEnthalpy = StmInt.hft(simpleMergeTemp);

            double addedEnergy = kg * (enthalpy - afterEnthalpy);

            double attemptedTemperature = waterTemperature + (addedEnergy / 4.200 / waterMass);
            double fraction = 1;
            if (attemptedTemperature > temperature)
            {
                fraction = (temperature - waterTemperature) / (attemptedTemperature - waterTemperature);
                if (fraction < 0)
                {
                    sim.HETemp += $" {Math.Round(waterTemperature, 1)} ";
                    return waterTemperature;
                }
                else
                    attemptedTemperature = waterTemperature + fraction * (attemptedTemperature - waterTemperature);
            }
            waterMass += kg * fraction;
            kg *= 1 - fraction;
            sim.HETemp += $" {Math.Round(attemptedTemperature, 1)} ";
            return attemptedTemperature;
        }

    }
}
