using System;
using System.Text;

namespace APDU
{
    public partial class VersionResponse : Response, IRawConvertible
    {
        public string Version => Encoding.UTF8.GetString(Body);

        public VersionResponse(string version)
        {
            Body = Encoding.UTF8.GetBytes(version);
            Trailer = ProtocolErrorCode.NoError;
        }
    }

    partial class VersionResponse
    {
        public override void Init(byte[] data, ProtocolErrorCode trailer)
        {
            Body = data;
            Trailer = trailer;
        }

        public override void ValidateBody()
        {
            if (Encoding.UTF8.GetBytes(Version).Length < 1)
            {
                throw ResponseError.WithError(ResponseErrorCode.BadSize);
            }

            if (Trailer != ProtocolErrorCode.NoError)
            {
                throw ResponseError.WithError(ResponseErrorCode.BadStatus);
            }
        }
    }
}