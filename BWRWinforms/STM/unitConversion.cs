using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamProperties
{
    public partial class StmProp
    {
        /* Unit conversion routines
         * 
         * Convert from SI to BTU if unit = 1
         * Convert from BTU to SI if unit = -1
         * 
         */

        double ConvertT(double tin, int unit)
        {
            return tin;            
        }
    }
}