using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using U2FLib.Storage;
using APDU;

namespace U2FLib
{
    partial class BackgroundTask
    {
        private IRawConvertible HandleVersionRequest(byte[] rawData, IO_CTL_XFER_MESSAGE request)
        {

            var _ = new VersionRequest(rawData); // validate request data;
            return new VersionResponse("U2F_V2");
        }

        private IRawConvertible HandleRegisterRequest(byte[] rawData, IO_CTL_XFER_MESSAGE request)
        {
            var req = new RegisterRequest(rawData);
            var facet = KnownFacets.GetKnownFacet(req.ApplicationParameter);
            var ss = Encoding.UTF8.GetString(req.ApplicationParameter);
            if (facet == "bogus")
            {
                return CreateError(ProtocolErrorCode.OtherError);
            }

            if (!UserPresence.Present)
            {
                UserPresence.AskAsync(UserPresence.PresenceType.Registration, facet);
                return CreateError(ProtocolErrorCode.ConditionNoSatisfied);
            }

            UserPresence.Take();
            U2FRegistration reg;
            try
            {
                reg = new U2FRegistration(req.ApplicationParameter);
            }
            catch
            {
                return CreateError(ProtocolErrorCode.OtherError);
            }

            var publicKey = reg.KeyPair.PublicKey;
            if (publicKey == null) return CreateError(ProtocolErrorCode.OtherError);

            var payloadSize = 1 + req.ApplicationParameter.Length + req.ChallengeParameter.Length +
                              reg.KeyHandle.Length + publicKey.Length;

            var sigPayload = new List<byte>(payloadSize);
            sigPayload.Add(0);
            sigPayload.AddRange(req.ApplicationParameter);
            sigPayload.AddRange(req.ChallengeParameter);
            sigPayload.AddRange(reg.KeyHandle);
            sigPayload.AddRange(publicKey);

            var sig = Signature.SignData(sigPayload.ToArray());
            var resp = new RegisterResponse(publicKey, keyHandle: reg.KeyHandle,
                certificate: Signature.GetCertificatePublicKeyInDer(), signature: sig);

            return resp;
        }

        private IRawConvertible HandleAuthenticationRequest(byte[] rawData, BackgroundTask.IO_CTL_XFER_MESSAGE request)
        {
            var req = new AuthenticationRequest(rawData);

            var reg = U2FRegistration.Find(keyHandle: req.KeyHandle, applicationParameter: req.ApplicationParameter);
            if (reg == null) return CreateError(ProtocolErrorCode.WrongData);

            if (req.Control == Control.CheckOnly) return CreateError(ProtocolErrorCode.ConditionNoSatisfied);

            var facet = KnownFacets.GetKnownFacet(req.ApplicationParameter);

            if (!UserPresence.Present)
            {
                UserPresence.AskAsync(UserPresence.PresenceType.Authentication, facet);
                return CreateError(ProtocolErrorCode.ConditionNoSatisfied);
            }

            UserPresence.Take();
            ApplicationData appData;
            using (var db = new AppDbContext())
            {
                appData = db.ApplicationDatum.First();
                if (appData == null) return CreateError(ProtocolErrorCode.OtherError);
                appData.Counter += 1;
                db.SaveChanges();
            }

            var payloadSize = req.ApplicationParameter.Length + 1 + Marshal.SizeOf<UInt32>() +
                              req.ApplicationParameter.Length;
            var sigPayload = new List<byte>(capacity: payloadSize);
            sigPayload.AddRange(req.ApplicationParameter);
            sigPayload.Add(0x01); // user present

            var counterBytes = BitConverter.GetBytes(appData.Counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);
            sigPayload.AddRange(counterBytes);

            sigPayload.AddRange(req.ChallengeParameter);

            try
            {
                var sig = Signature.SignData(sigPayload.ToArray(), reg.KeyPair.PrivateKey);
                return new AuthenticationResponse(userPresence: 0x01, counter: appData.Counter, sig);
            }
            catch
            {
                return CreateError(ProtocolErrorCode.OtherError);
            }
            
        }

        private IRawConvertible CreateError(ProtocolErrorCode code)
        {
            return new ErrorResponse(code);
        }
    }
}
