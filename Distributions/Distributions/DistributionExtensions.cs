using Distributions.Distributions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random;

namespace Distributions.Distributions {
    public static class DistributionExtensions {

        // Зиккурат алгоритм

        #region Consts

        /// <summary>
        /// Number of blocks.
        /// </summary>
        const int __blockCount = 128;

        /// <summary>
        /// Right hand x coord of the base rectangle, thus also the left hand x coord of the tail 
        /// (pre-determined/computed for 128 blocks).
        /// </summary>
        const double __R = 3.442619855899;

        /// <summary>
        /// Area of each rectangle (pre-determined/computed for 128 blocks).
        /// </summary>
        const double __A = 9.91256303526217e-3;

        /// <summary>
        /// Denominator for __INCR constant. This is the number of distinct values this class is capable 
        /// of generating in the interval [0,1], i.e. (2^53)-1 distinct values.
        /// </summary>
        const ulong __MAXINT = (1UL << 53) - 1;

        /// <summary>
        /// Scale factor for converting a ULong with interval [0, 0x1f_ffff_ffff_ffff] to a double with interval [0,1].
        /// </summary>
        const double __INCR = 1.0 / __MAXINT;

        #endregion

        #region Static Fields

        // __x[i] and __y[i] describe the top-right position of rectangle i.
        static readonly double[] __x;
        static readonly double[] __y;

        // The proportion of each segment that is entirely within the distribution, expressed as ulong where 
        // a value of 0 indicates 0% and 2^53-1 (i.e. 53 binary 1s) 100%. Expressing this as an integer value 
        // allows some floating point operations to be replaced with integer operations.
        static readonly ulong[] __xComp;

        // Useful precomputed values.
        // Area A divided by the height of B0. Note. This is *not* the same as __x[i] because the area 
        // of B0 is __A minus the area of the distribution tail.
        static readonly double __A_Div_Y0;

        #endregion

        #region Static Initialiser

        static DistributionExtensions() {
            // Initialise rectangle position data. 
            // __x[i] and __y[i] describe the top-right position of Box i.

            // Allocate storage. We add one to the length of _x so that we have an entry at __x[__blockCount], this avoids having 
            // to do a special case test when sampling from the top box.
            __x = new double[__blockCount + 1];
            __y = new double[__blockCount];

            // Determine top right position of the base rectangle/box (the rectangle with the Gaussian tale attached). 
            // We call this Box 0 or B0 for short.
            // Note. x[0] also describes the right-hand edge of B1. (See diagram).
            __x[0] = __R;
            __y[0] = GaussianPdfDenorm(__R);

            // The next box (B1) has a right hand X edge the same as B0. 
            // Note. B1's height is the box area divided by its width, hence B1 has a smaller height than B0 because
            // B0's total area includes the attached distribution tail.
            __x[1] = __R;
            __y[1] = __y[0] + (__A / __x[1]);

            // Calc positions of all remaining rectangles.
            for (int i = 2; i < __blockCount; i++) {
                __x[i] = GaussianPdfDenormInv(__y[i - 1]);
                __y[i] = __y[i - 1] + (__A / __x[i]);
            }

            // For completeness we define the right-hand edge of a notional box 6 as being zero (a box with no area).
            __x[__blockCount] = 0.0;

            // Useful precomputed values.
            __A_Div_Y0 = __A / __y[0];
            __xComp = new ulong[__blockCount];

            // Special case for base box. __xComp[0] stores the area of B0 as a proportion of __R 
            // (recalling that all segments have area __A, but that the base segment is the combination of B0 and the distribution tail).
            // Thus __xComp[0] is the probability that a sample point is within the box part of the segment.
            __xComp[0] = (ulong)(((__R * __y[0]) / __A) * (double)__MAXINT);

            for (int i = 1; i < __blockCount - 1; i++) {
                __xComp[i] = (ulong)((__x[i + 1] / __x[i]) * (double)__MAXINT);
            }
            __xComp[__blockCount - 1] = 0;  // Shown for completeness.

            // Sanity check. Test that the top edge of the topmost rectangle is at y=1.0.
            // Note. We expect there to be a tiny drift away from 1.0 due to the inexactness of floating
            // point arithmetic.
            Debug.Assert(Math.Abs(1.0 - __y[__blockCount - 1]) < 1e-10);
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Take a sample from the standard Gaussian distribution, i.e. with mean of 0 and standard deviation of 1.
        /// </summary>
        /// <returns>A random sample.</returns>
        public static double Sample(this IRandomSource rng) {
            for (; ; )
            {
                // Generate 64 random bits.
                ulong u = rng.NextULong();

                // Note. 61 random bits are required and therefore the lowest three bits are discarded
                // (a typical characteristic of PRNGs is that the least significant bits exhibit lower
                // quality randomness than the higher bits).
                // Select a segment (7 bits, bits 3 to 9).
                int s = (int)((u >> 3) & 0x7f);

                // Select sign bit (bit 10).
                double sign = ((u & 0x400) == 0) ? 1.0 : -1.0;

                // Get a uniform random value with interval [0, 2^53-1], or in hexadecimal [0, 0x1f_ffff_ffff_ffff] 
                // (i.e. a random 53 bit number) (bits 11 to 63).
                ulong u2 = u >> 11;

                // Special case for the base segment.
                if (0 == s) {
                    if (u2 < __xComp[0]) {
                        // Generated x is within R0.
                        return u2 * __INCR * __A_Div_Y0 * sign;
                    }
                    // Generated x is in the tail of the distribution.
                    return SampleTail(rng) * sign;
                }

                // All other segments.
                if (u2 < __xComp[s]) {
                    // Generated x is within the rectangle.
                    return u2 * __INCR * __x[s] * sign;
                }

                // Generated x is outside of the rectangle.
                // Generate a random y coordinate and test if our (x,y) is within the distribution curve.
                // This execution path is relatively slow/expensive (makes a call to Math.Exp()) but is relatively rarely executed,
                // although more often than the 'tail' path (above).
                double x = u2 * __INCR * __x[s];
                if (__y[s - 1] + ((__y[s] - __y[s - 1]) * rng.NextDouble()) < GaussianPdfDenorm(x)) {
                    return x * sign;
                }
            }
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Sample from the distribution tail (defined as having x >= __R).
        /// </summary>
        /// <returns></returns>
        private static double SampleTail(IRandomSource rng) {
            double x, y;
            do {
                // Note. we use NextDoubleNonZero() because Log(0) returns NaN and will also tend to be a very slow execution path (when it occurs, which is rarely).
                x = -Math.Log(rng.NextDoubleNonZero()) / __R;
                y = -Math.Log(rng.NextDoubleNonZero());
            }
            while (y + y < x * x);
            return __R + x;
        }

        /// <summary>
        /// Gaussian probability density function, denormalised, that is, y = e^-(x^2/2).
        /// </summary>
        private static double GaussianPdfDenorm(double x) {
            return Math.Exp(-(x * x / 2.0));
        }

        /// <summary>
        /// Inverse function of GaussianPdfDenorm(x)
        /// </summary>
        private static double GaussianPdfDenormInv(double y) {
            // Operates over the y interval (0,1], which happens to be the y interval of the pdf, 
            // with the exception that it does not include y=0, but we would never call with 
            // y=0 so it doesn't matter. Note that a Gaussian effectively has a tail going
            // into infinity on the x-axis, hence asking what is x when y=0 is an invalid question
            // in the context of this class.
            return Math.Sqrt(-2.0 * Math.Log(y));
        }

        #endregion


        // Преобразование Бокса-Мюллера

        public static double BoxMullerDouble(this IRandomSource r) {
            double x, y, s;

            do {
                x = 2.0 * r.NextDouble() - 1.0;
                y = 2.0 * r.NextDouble() - 1.0;
                s = x * x + y * y;
            }
            while (s >= 1.0);

            double fac = Math.Sqrt(-2.0 * Math.Log(s) / s);
            return x * fac;
        }
    }
}
