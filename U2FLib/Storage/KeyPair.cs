using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace U2FLib.Storage
{
    public class KeyPair
    {
        private static readonly string ECDSA = "ECDSA";
        private static readonly string NISTP256 = "P-256";
        private static readonly SecureRandom secureRandom = new SecureRandom();
        private static readonly X9ECParameters curve = NistNamedCurves.GetByName(NISTP256);

        private static readonly ECDomainParameters domain =
            new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

        private byte[] _applicationTag;
        private byte[] _privateKey;
        private byte[] _publicKey;

        private bool dataProtected => !Environment.GetCommandLineArgs().Contains("--db-unprotected");

        public KeyPair()
        {
        }

        // Generate a KeyPair with given label
        public KeyPair(string label)
        {
            Label = label;

            var g = new ECKeyPairGenerator(ECDSA);
            var gParams = new ECKeyGenerationParameters(domain, secureRandom);
            g.Init(gParams);
            var keyPair = g.GenerateKeyPair();

            PrivateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private).GetDerEncoded();

            var ecPublicKey = (ECPublicKeyParameters)keyPair.Public;
            PublicKey = ecPublicKey.Q.GetEncoded();

            KeyHandle = Convert.ToBase64String(sha512(PublicKey));

        }

        public string Label { get; set; }

        [Key] public string KeyHandle { get; set; }

        public byte[] ApplicationTag
        {
            get => UnProtect(_applicationTag);
            set => _applicationTag = Protect(value);
        }

        public byte[] PublicKey
        {
            get => UnProtect(_publicKey);
            set => _publicKey = Protect(value);
        }

        public byte[] PrivateKey
        {
            get => UnProtect(_privateKey);
            set => _privateKey = Protect(value);
        }

        private byte[] Protect(byte[] data)
        {
            if (!dataProtected) return data;

            try
            {
                return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            }
            catch
            {
                return null;
            }
        }

        private byte[] UnProtect(byte[] data)
        {
            if (!dataProtected) return data;

            try
            {
                return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            }
            catch
            {
                return null;
            }
        }

        private static byte[] sha512(byte[] data)
        {
            using (var hasher = new SHA512Managed())
            {
                return hasher.ComputeHash(data);
            }
        }
    }
}