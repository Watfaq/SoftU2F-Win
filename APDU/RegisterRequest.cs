using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public partial class RegisterRequest : Command, IRawConvertible
    {
        public byte[] ChallengeParameter
        {
            get
            {
                var lowerBound = 0;
                return Body.Skip(lowerBound).Take(Constants.U2F_CHAL_SIZE).ToArray();
            }
        }

        public byte[] ApplicationParameter
        {
            get
            {
                var lowerBound = Constants.U2F_CHAL_SIZE;
                return Body.Skip(lowerBound).Take(Constants.U2F_APPID_SIZE).ToArray();
            }
        }

        public RegisterRequest(byte[] challengeParameter, byte[] applicationParameter)
        {
            var stream = new MemoryStream();

            using (var writer = new DataWriter(stream))
            {
                writer.WriteBytes(challengeParameter);
                writer.WriteBytes(applicationParameter);
            }

            Body = stream.ToArray();
            Header = new CommandHeader(ins: CommandCode.Register, dataLength: Body.Length);
            Trailer = new CommandTrailer(noBody: false);
        }
    }

    partial class RegisterRequest
    {
        public RegisterRequest(byte[] raw) : base(raw)
        {
        }

        public override void ValidateBody()
        {
            if (Body.Length != Constants.U2F_CHAL_SIZE + Constants.U2F_APPID_SIZE)
            {
                throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);
            }
        }
    }

}
