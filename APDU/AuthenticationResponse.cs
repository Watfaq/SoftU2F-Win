using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace APDU
{
    public partial class AuthenticationResponse: Response, IRawConvertible
    {
        public byte UserPresence => Body[0];

        public UInt32 Counter => BitConverter.ToUInt32(Body.Skip(1).Take(4).Reverse().ToArray(), 0);

        public byte[] Signature => Body.Skip(1 + 4).ToArray();

        public AuthenticationResponse(byte userPresence, UInt32 counter, byte[] signature)
        {
            var stream = new MemoryStream();
            using (var writer = new DataWriter(stream))
            {
                writer.WriteByte(userPresence);
                writer.WriteUInt32(counter);
                writer.WriteBytes(signature);
            }

            Body = stream.ToArray();
            Trailer = ProtocolErrorCode.NoError;
        }
    }

    partial class AuthenticationResponse
    {
        public AuthenticationResponse(byte[] data, ProtocolErrorCode trailer)
        {
            Init(data, trailer);
        }

        public override void Init(byte[] data, ProtocolErrorCode trailer)
        {
            Body = data;
            Trailer = trailer;
        }

        public override void ValidateBody()
        {
            // assuming 1 is the minimum signature size
            if (Body.Length < 1 + 4 + 1) throw ResponseError.WithError(ResponseErrorCode.BadSize);

            if (Trailer != ProtocolErrorCode.NoError) throw ResponseError.WithError(ResponseErrorCode.BadStatus);
        }
    }
}
