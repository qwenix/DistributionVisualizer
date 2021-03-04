using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Distributions.Distributions {
    public static class AdvancedMath {
        // Constants
        private const double a1 = 0.254829592;
        private const double a2 = -0.284496736;
        private const double a3 = 1.421413741;
        private const double a4 = -1.453152027;
        private const double a5 = 1.061405429;
        private const double p = 0.3275911;

        public static double Erf(double x) {
            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }
    }
}
