using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SoftU2F.Console.Storage;

namespace U2FHID
{
    public class U2FRegistration
    {
        private static readonly string KP_Lable = "SoftU2F Security Key";

        public KeyPair KeyPair;
        public byte[] ApplicationParameter;
        public byte[] KeyHandle => Convert.FromBase64String(KeyPair.KeyHandle);

        public U2FRegistration(byte[] applicationParameter)
        {
            using var context = new AppDbContext();
            var kp = new KeyPair(KP_Lable) { ApplicationTag = applicationParameter };
            context.KeyPairs.Add(kp);
            context.SaveChanges();
            KeyPair = kp;
        }

        public U2FRegistration(KeyPair keyPair, byte[] applicationParameter)
        {
            KeyPair = keyPair;
            ApplicationParameter = applicationParameter;
        }

        public static U2FRegistration Find(byte[] keyHandle, byte[] applicationParameter)
        {
            var sKeyHandle = Convert.ToBase64String(keyHandle);
            using var context = new AppDbContext();
            var kp = context.KeyPairs.SingleOrDefault(p => p.KeyHandle == sKeyHandle);
            if (kp == null) return null;

            var ap = kp.ApplicationTag;
            return !applicationParameter.SequenceEqual(ap) ? null : new U2FRegistration(kp, applicationParameter);
        }
    }
}
