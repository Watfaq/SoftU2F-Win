using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace APDU
{
    public partial class RegisterResponse : Response, IRawConvertible
    {
        private short Reserved => Body.Skip(ReservedRange.Start).Take(ReservedRange.Length).First();

        private int keyHandleLength =>
            (int)Body.Skip(KeyHandleLengthRange.Start).Take(KeyHandleLengthRange.Length).First();

        private (int Start, int Length) ReservedRange => (0, 1);

        private (int Start, int Length) PublicKeyRange =>
            (ReservedRange.Start + ReservedRange.Length, Constants.U2F_EC_POINT_SIZE);

        private (int Start, int Length) KeyHandleLengthRange => (ReservedRange.Start + ReservedRange.Length, 1);

        private (int Start, int Length) KeyHandleRange => (KeyHandleLengthRange.Start + KeyHandleLengthRange.Length,
            keyHandleLength);

        private int certificateSize => Signature.GetCertificatePublicKeyInDer().Length;

        private (int Start, int Length) CertificateRange =>
            (KeyHandleRange.Start + KeyHandleRange.Length, certificateSize);

        private (int Start, int Length) SignatureRange => (CertificateRange.Start + CertificateRange.Length,
            Body.Length - ReservedRange.Length - PublicKeyRange.Length - KeyHandleLengthRange.Length -
            KeyHandleRange.Length - CertificateRange.Length);

        public RegisterResponse(byte[] publicKey, byte[] keyHandle, byte[] certificate, byte[] signature)
        {
            var stream = new MemoryStream();
            using (var writer = new DataWriter(stream))
            {
                writer.WriteByte((byte)0x05);
                writer.WriteBytes(publicKey);
                writer.WriteByte((byte)keyHandle.Length);
                writer.WriteBytes(keyHandle);
                writer.WriteBytes(certificate);
                writer.WriteBytes(signature);
            }

            Body = stream.ToArray();
            Trailer = ProtocolErrorCode.NoError;
        }
    }
    partial class RegisterResponse
    {
        public override void Init(byte[] body, ProtocolErrorCode trailer)
        {
            Body = body;
            Trailer = trailer;
        }

        public override void ValidateBody()
        {
            var min = Marshal.SizeOf<byte>() + Constants.U2F_EC_POINT_SIZE + Marshal.SizeOf<byte>();
            if (Body.Length < min) throw ResponseError.WithError(ResponseErrorCode.BadSize);


            min += keyHandleLength + 1;
            if (Body.Length < min) throw ResponseError.WithError(ResponseErrorCode.BadSize);

            if (certificateSize == 0) throw ResponseError.WithError(ResponseErrorCode.BadCertificate);

            min += certificateSize + 1;
            if (Body.Length < min) throw ResponseError.WithError(ResponseErrorCode.BadSize);

            if (Reserved != 0x05) throw ResponseError.WithError(ResponseErrorCode.BadData);

            if (Trailer != ProtocolErrorCode.NoError) throw ResponseError.WithError(ResponseErrorCode.BadStatus);
        }
    }
}
