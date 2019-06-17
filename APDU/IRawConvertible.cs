using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public interface IRawConvertible
    {
        byte[] Raw { get; }

        void Init(byte[] raw);
    }
}
