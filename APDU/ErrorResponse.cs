using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public partial class ErrorResponse : Response, IRawConvertible
    {
        public ErrorResponse(ProtocolErrorCode trailer, byte[] body=default)
        {
            Init(body, trailer);
        }
    }

    public partial class ErrorResponse
    {
        public override void Init(byte[] data, ProtocolErrorCode trailer)
        {
            Body = default;
            Trailer = trailer;
        }

        public override void ValidateBody()
        {
            if (Body.Length != 0)
            {
                throw ResponseError.WithError(ResponseErrorCode.BadSize);
            }
        }
    }
}
