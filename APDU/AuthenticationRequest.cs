using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace APDU
{
    public partial class AuthenticationRequest: Command, IRawConvertible
    {
        public Control Control 
        {
            get
            {
                if (Enum.IsDefined(typeof(Control), Header.p1))
                {
                    return (Control) Header.p1;
                }

                return Control.Invalid;
            }
        }

        public byte[] ChallengeParameter => Body.Skip(0).Take(Constants.U2F_CHAL_SIZE).ToArray();

        public byte[] ApplicationParameter =>
            Body.Skip(Constants.U2F_CHAL_SIZE).Take(Constants.U2F_APPID_SIZE).ToArray();

        private int keyHandleLength => Body[Constants.U2F_CHAL_SIZE + Constants.U2F_APPID_SIZE];

        public byte[] KeyHandle => Body.Skip(Constants.U2F_CHAL_SIZE + Constants.U2F_APPID_SIZE + 1)
            .Take(keyHandleLength).ToArray();

        public AuthenticationRequest(byte[] challengeParameter, byte[] applicationParameter, byte[] keyHandle,
            Control control)
        {
            var stream = new MemoryStream();
            using (var writer = new DataWriter(stream))
            {
                writer.WriteBytes(challengeParameter);
                writer.WriteBytes(applicationParameter);
                writer.WriteByte((byte)keyHandle.Length);
                writer.WriteBytes(keyHandle);
            }

            Body = stream.ToArray();
            Header = new CommandHeader(ins: CommandCode.Authenticate, p1: (byte) control, dataLength: Body.Length);
            Trailer = new CommandTrailer(noBody: true);

        }
    }

    partial class AuthenticationRequest
    {
        public AuthenticationRequest(byte[] raw) : base(raw)
        {
        }

        public override void ValidateBody()
        {
            if (Body.Length < Constants.U2F_CHAL_SIZE + Constants.U2F_APPID_SIZE + 1)
                throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);

            // to fit Conformance test..
            if (keyHandleLength < 64) throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);

            if (Body.Length != Constants.U2F_CHAL_SIZE + Constants.U2F_APPID_SIZE + 1 + keyHandleLength)
                throw ProtocolError.WithCode(ProtocolErrorCode.WrongLength);

            if (Control == Control.Invalid)
            {
                throw ProtocolError.WithCode(ProtocolErrorCode.OtherError);
            }
        }
    }
}
