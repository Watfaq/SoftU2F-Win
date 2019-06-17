using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public enum DataReaderErrorCode:byte
    {
        End = 0
    }

    public class DataReaderError : Exception
    {
        public DataReaderErrorCode ErrorCode;

        private DataReaderError(DataReaderErrorCode code)
        {
            ErrorCode = code;
        }

        public static DataReaderError WithCode(DataReaderErrorCode code)
        {
            return new DataReaderError(code);
        }
    }
    public class DataReader
    {
        private readonly byte[] data;
        private int _offset;

        public int Remaining => data.Length - _offset;

        public DataReader(byte[] data, int offset = 0) 
        {
            this.data = data;
            this._offset = offset;
        }

        public byte ReadByte()
        {
            var rv = data.Skip(_offset).Take(1).First();
            _offset += 1;
            return rv;
        }

        public ushort ReadUInt16()
        {
            var bytes = data.Skip(_offset).Take(2).ToArray();
            _offset += 2;
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public uint ReadUInt32()
        {
            var bytes = data.Skip(_offset).Take(4).ToArray();
            _offset += 4;
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public byte[] ReadBytes(int n)
        {
            if (n > Remaining) throw DataReaderError.WithCode(DataReaderErrorCode.End);

            var rv = data.Skip(_offset).Take(n).ToArray();
            _offset += n;
            return rv;
        }
    }
}
