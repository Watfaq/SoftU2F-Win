using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public class VersionRequest: Command, IRawConvertible
    {
        public VersionRequest(byte[] raw) : base(raw)
        {
        }

        public override void ValidateBody()
        {
            if (Body.Length > 0)
            {
                throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);
            }
        }
    }
}
