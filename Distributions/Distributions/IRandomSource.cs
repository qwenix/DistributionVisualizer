using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Distributions.Distributions {
    public interface IRandomSource {
        double NextDouble();
        ulong NextULong();
        double NextDoubleNonZero();
    }
}
