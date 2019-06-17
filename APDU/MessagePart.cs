using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public abstract class MessagePart
    {
        public abstract void Init(DataReader reader);

        public void Init(byte[] raw)
        {
            var reader = new DataReader(raw);
            Init(reader);
        }
    }
}
