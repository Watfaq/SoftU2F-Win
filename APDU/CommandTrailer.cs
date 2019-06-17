using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public class ProtocolError : Exception
    {
        public ProtocolErrorCode ErrorCode;

        private ProtocolError(ProtocolErrorCode code)
        {
            ErrorCode = code;
        }

        public static ProtocolError WithCode(ProtocolErrorCode code)
        {
            return new ProtocolError(code);
        }
    }

    public partial class CommandTrailer: MessagePart, IRawConvertible
    {
        private int MaxResponse;
        private bool NoBody;

      
        public CommandTrailer(DataReader reader)
        {
            Init(reader);
        }

        public CommandTrailer(bool noBody, int maxResponse = Constants.MaxResponseSize)
        {
            NoBody = noBody;
            MaxResponse = maxResponse;
        }
    }

    partial class CommandTrailer
    {
        public byte[] Raw
        {
            get
            {
                using (var stream = new MemoryStream())
                using (var writer = new DataWriter(stream))
                {
                    if (NoBody)
                    {
                        writer.WriteByte((byte)(0x00));
                    }

                    if (MaxResponse < UInt16.MaxValue)
                    {
                        writer.WriteUint16(Convert.ToUInt16(MaxResponse));
                    }
                    else
                    {
                        writer.WriteUint16(Convert.ToUInt16(0x00));
                    }

                    return stream.ToArray();
                }
            }
        }

        public override void Init(DataReader reader)
        {
            if (reader.Remaining == 3)
            {
                NoBody = true;
                var zero = reader.ReadByte();
                if (zero != 0x00)
                {
                    throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);
                }
            }
            else
            {
                NoBody = false;
            }

            switch (reader.Remaining)
            {
                case 0:
                    MaxResponse = 0;
                    break;
                case 1:
                    throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);
                case 2:
                    var mr = reader.ReadUInt16();
                    MaxResponse = mr == 0x0000 ? Constants.MaxResponseSize : mr;

                    break;
                default:
                    throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);
            }
        }
    }
}
