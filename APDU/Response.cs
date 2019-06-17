using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public class ResponseError : Exception
    {
        public ResponseErrorCode ErrorCode;
        ResponseError(ResponseErrorCode code)
        {
            ErrorCode = code;
        }

        public static ResponseError WithError(ResponseErrorCode code)
        {
            return new ResponseError(code);
        }
    }

    public abstract partial class Response
    {
        public byte[] Body { get; internal set; }
        public ProtocolErrorCode Trailer { get; internal set; }

        public abstract void Init(byte[] data, ProtocolErrorCode trailer);

        public abstract void ValidateBody();
    }

    // Implement IRawConvertible

    public partial class Response
    {
        public byte[] Raw
        {
            get
            {
                var stream = new MemoryStream();
                using(var writer = new DataWriter(stream))
                {
                    if (Body != default)
                    {
                        writer.WriteBytes(Body);
                    }
                    writer.WriteUint16((ushort)Trailer);
                }
                return stream.ToArray();
            }
        }

        public  void Init(byte[] raw)
        {
            var reader = new DataReader(raw);
            var body = reader.ReadBytes(reader.Remaining - 2);
            var trailer = (ProtocolErrorCode) reader.ReadUInt16();

            Init(body, trailer);

            ValidateBody();
        }
    }
}
