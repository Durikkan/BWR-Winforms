using SteamProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BWRWinforms
{
    internal static class StmInt
    {
        static readonly StmProp stmProp = new StmProp();
        const double kelvin = 273.15;
        internal static double hgp(double pressure)
        {
            int stat = 0;
            var result = stmProp.hgp(pressure, ref stat, 0);
            return result;
        }

        internal static double hfp(double pressure)
        {
            int stat = 0;
            var result = stmProp.hfp(pressure, ref stat, 0);
            return result;
        }

        internal static double hft(double celcius)
        {
            int stat = 0;
            var result = stmProp.hft(celcius + kelvin, ref stat, 0);
            return result;
        }

        internal static double vft(double waterTemp)
        {
            int stat = 0;
            var result = stmProp.vft(waterTemp + kelvin, ref stat, 0);
            return result;
        }
        internal static double hpt(double remainingPressure, double temperature)
        {
            int stat = 0;
            var result = stmProp.hpt(remainingPressure, temperature + kelvin, ref stat, 0);
            return result;
        }

        internal static double Psat(double waterTemp)
        {
            int stat = 0;
            var result = stmProp.Psat(waterTemp + kelvin, ref stat, 0);
            return result;
        }

        internal static double Tsat(double steamPipePressure)
        {
            int stat = 0;
            var result = stmProp.Tsat(steamPipePressure, ref stat, 0) - kelvin;
            return result;
        }

        internal static double Tph(double remainingPressure, double enthalpy)
        {
            int stat = 0;
            double result = stmProp.Tph(remainingPressure, enthalpy, ref stat, 0) - kelvin;
            return result;
        }


    }
}
