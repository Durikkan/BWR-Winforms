using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BWRWinforms
{
    internal static class Utility
    {
        internal static double MoveTowards(double start, double target, double rate)
        {
            if (start > target)
            {
                start -= rate;
                if (start < target)
                    start = target;
                return start;
            }
            else if (start < target)
            {
                start += rate;
                if (start > target)
                    start = target;
                return start;
            }
            return start;
        }

        internal static double Lerp(double first, double second, double by)
        {
            return first * (1 - by) + second * by;
        }

        public static string ToSI(double d, bool capToZero = false)
        {
            char[] incPrefixes = new[] { 'k', 'M', 'G', 'T', };
            char[] decPrefixes = new[] { 'm', '\u03bc', 'n', 'p', };

            int degree = (int)Math.Floor(Math.Log10(Math.Abs(d)) / 3);
            degree = Math.Min(degree, 3);
            degree = Math.Max(degree, capToZero ? 0 : -4);
            double scaled = d * Math.Pow(1000, -degree);

            char? prefix = null;
            switch (Math.Sign(degree))
            {
                case 1: prefix = incPrefixes[degree - 1]; break;
                case -1: prefix = decPrefixes[-degree - 1]; break;
            }
            double abs = Math.Abs(scaled);
            if (abs >= 100)
                return scaled.ToString("000.0") + " " + prefix;
            else if (abs >= 10)
                return scaled.ToString("00.00") + " " + prefix;
            else
                return scaled.ToString("0.000") + " " + prefix;
        }
    }
}
