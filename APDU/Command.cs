using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{

    public abstract partial class Command
    {
        public static CommandCode CommandType(byte[] data)
        {
            var reader = new DataReader(data);
            var header = new CommandHeader(reader);
            return header.ins;
        }

        public CommandHeader Header { get; set; }
        public byte[] Body { get; set; }
        public CommandTrailer Trailer { get; set; }

        public abstract void ValidateBody();
    }

    partial class Command
    {
        public byte[] Raw
        {
            get
            {
                using (var stream = new MemoryStream())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(Header.Raw);
                    writer.Write(Body);
                    writer.Write(Trailer.Raw);
                    return stream.ToArray();
                }
            }
        }

        public void Init(byte[] raw)
        {
            var reader = new DataReader(raw);
            CommandHeader header;
            byte[] body;
            CommandTrailer trailer;

            try
            {
                header = new CommandHeader(reader);
                body = reader.ReadBytes(header.dataLength);
                trailer = new CommandTrailer(reader);
            }
            catch (DataReaderError e)
            {
                if (e.ErrorCode == DataReaderErrorCode.End) throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);
                throw;
            }

            Init(header, body, trailer);

            ValidateBody();
        }

        protected Command(byte[] raw)
        {
            Init(raw);
        }

        protected Command()
        {
        }


        internal void Init(CommandHeader header, byte[] body, CommandTrailer trailer)
        {
            Header = header;
            Body = body;
            Trailer = trailer;
        }
    }
}
