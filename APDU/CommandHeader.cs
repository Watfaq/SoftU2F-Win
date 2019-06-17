using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public partial class CommandHeader : MessagePart, IRawConvertible
    {
        public CommandClass cla;
        public CommandCode ins;
        public byte p1;
        public byte p2;
        public int dataLength;

        public byte[] Raw
        {
            get
            {
                using (var output = new MemoryStream())
                using (var writer = new DataWriter(output))
                {
                    writer.WriteByte((byte)cla);
                    writer.WriteByte((byte)ins);
                    writer.WriteByte(p1);
                    writer.WriteByte(p2);

                    if (dataLength > 0)
                    {
                        writer.WriteByte((byte)0x0);
                        writer.WriteUint16(Convert.ToUInt16(dataLength));
                    }

                    return output.ToArray();
                }
            }
        }


        public CommandHeader(DataReader reader)
        {
            Init(reader);
        }

        public CommandHeader(CommandCode ins, int dataLength, byte p1 = 0x00, byte p2 = 0x00, CommandClass cla = CommandClass.Reserved)
        {
            this.cla = cla;
            this.ins = ins;
            this.p1 = p1;
            this.p2 = p2;
            this.dataLength = dataLength;
        }
    }

    partial class CommandHeader
    {
        public override void Init(DataReader reader)
        {
            try
            {
                var claByte = reader.ReadByte();
                if (!Enum.IsDefined(typeof(CommandClass), claByte))
                    throw ProtocolError.WithCode(ProtocolErrorCode.ClassNotSupported);
                cla = (CommandClass)claByte;

                var insByte = reader.ReadByte();
                if (!Enum.IsDefined(typeof(CommandCode), insByte))
                    throw ProtocolError.WithCode(ProtocolErrorCode.InsNotSupported);
                ins = (CommandCode)insByte;

                p1 = reader.ReadByte();
                p2 = reader.ReadByte();

                switch (reader.Remaining)
                {
                    case 0:
                    case 1:
                    case 2:
                        throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);
                    case 3:
                        dataLength = 0;
                        break;

                    default:
                        var lc0 = reader.ReadByte();
                        if (lc0 != 0x00) throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);

                        var lc = reader.ReadUInt16();
                        dataLength = lc;
                        break;
                }
            }
            catch (EndOfStreamException)
            {
                throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);
            }
        }
    }
}
