using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    class DataWriter : IDisposable
    {
        private readonly Stream _stream;
        private readonly BinaryWriter _writer;

        public DataWriter(Stream stream)
        {
            this._stream = stream;
            _writer = new BinaryWriter(stream);
        }

        public void WriteByte(byte b)
        {
            _writer.Write(b);
        }

        public void WriteUint16(UInt16 b)
        {
            _writer.Write(BitConverter.GetBytes(b).Reverse().ToArray());
        }

        public void WriteUInt32(UInt32 b)
        {
            _writer.Write(BitConverter.GetBytes(b).Reverse().ToArray());
        }

        public void WriteBytes(byte[] bs)
        {
            _writer.Write(bs);
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
