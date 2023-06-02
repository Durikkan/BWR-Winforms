using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace BWRWinforms
{
    internal class PIDController
    {
        
        public PIDController(double PGain, double IGain, double DGain)
        {            
            pGain = PGain;
            iGain = IGain;
            dGain = DGain;
        }

        double pGain = 0;
        double iGain = 0;
        double dGain = 0;
        double i = 0;
        double previous = 0;

        public void ResetIntegral(double value)
        {
            i = value;           
        }

        public double CalculateOutput(double current, double target)
        {
            double error = target - current;
           
            double p = pGain * error;
            i += iGain * error * 0.1;
            i = Clamp(i);

            double d = dGain * ((current - previous) / 0.1);

            double output = p + i - d;

            output = Clamp(output);

            previous = current;

            return output;
        }      

       

        private double Clamp(double num)
        {
            if (num < 0) { return 0; }
            if (num > 1) { return 1; }
            return num;
        }
    }
}

