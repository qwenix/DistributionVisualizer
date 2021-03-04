using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Distributions.Distributions {
    class MyDistribution {
        private const int RAND_MIN = 0;
        private const int RAND_MAX = 1;

        public static MyRandom Random = new MyRandom();
        
        public static double Uniform(double a, double b) {
            return a + Random.Next(RAND_MIN, RAND_MAX) * (b - a) / RAND_MAX;
        }

        public static double Exponential(double rate) {
            return Random.Sample() / rate;
        }
    }

    public class MyRandom : IRandomSource {
        public static Random Random = new Random(0);

        public double Next() {
            return Random.Next();
        }

        public double Next(double min, double max) {
            return Random.Next();
        }

        public double NextDouble() {
            return Random.NextDouble();
        }

        public double NextDoubleNonZero() {
            var a = Random.NextDouble();
            while (a == 0) {
                a = Random.NextDouble();
            }
            return a;
        }

        public ulong NextULong() {
            return (ulong)Random.Next();
        }
    }
}
