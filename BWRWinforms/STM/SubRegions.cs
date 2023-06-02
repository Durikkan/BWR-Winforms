using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamProperties
{

    public partial class StmProp
    {

        public int SubRegion(ref double p, ref double t)
        {
            // returns the region
            // t is temperature in K
            // p is pressure in kPa

            if (t == state.t && p == state.p)
                return state.region;
            
            int region=-1;


            if (p > 100000.0)
            {
                errorCondition = 1;
                AddErrorMessage("Pressure is out of bounds, Results are for 100 MPa");
                p = 100000.0;
            }
            else if (p < 0.0)
            {
                errorCondition = 1;
                AddErrorMessage("Pressure is out of bounds, Results are for 0");
                p = 0.0;
            }

            if (t < 273.15)
            {
                errorCondition = 1;
                AddErrorMessage("Temperature is out of bounds, Results are for 273.15 K");
                t = 273.15;
                region = 1;
            }
            else if (t < 623.15)
            {
                // calculate saturated pressure
                double psat = Pi_4(t);
                if (p < psat) 
                    region = 2;
                else
                    region = 1;
            }
            else if (t < 863.15)
            {
                // calculate boundary between regions 2 and 3

                double p23 = P23(t);

                if (p < p23)
                    region = 2;
                else
                    region = 3;
            }
            else if (t < 1073.15)
            {
                region = 2;
            }
            else if (t < 2273.15)
            {
                if (p > 50000.0)
                {
                    errorCondition = 1;
                    AddErrorMessage("Pressure is out of bounds, Results are for 50 MPa");
                    p = 50000.0;
                    region = 5;
                }
                region = 5;
            }
            else
            {
                errorCondition = 1;
                AddErrorMessage("Temperature is out of bounds, Results are for 2273.15 K");
                if (p > 50000.0)
                {
                    AddErrorMessage("Pressure is out of bounds, Results are for 50 MPa");
                    p = 50000.0;
                }
                t = 2273.15;
                region = 5;
            }

            state.region = region;
            return region;
        }
    }
}