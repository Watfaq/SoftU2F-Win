using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace APDU
{
    public static class Signature
    {
        private const string CertificatePem =
            "-----BEGIN CERTIFICATE-----\nMIICgTCCAWmgAwIBAgIUZMbG0ZhgV1gTP0OfWBZdzq1Prm0wDQYJKoZIhvcNAQEL\nBQAwADAeFw0xOTA2MTYxNzE0MTJaFw0xOTEyMTMxNzE0NDJaMEMxEzARBgNVBAYT\nCkV2ZXJ5d2hlcmUxFDASBgNVBAoTC1dhdGZhcSBJbmMuMRYwFAYDVQQDEw1Tb2Z0\nVTJGU2lnbmVyMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgr1NR5EjhQTS1B8s\nqrPmjN0cyPdKX/a2+wbGNGvlcPOBjU/htUopEuJmcovu3WaueVQlJ9F9pbmyKVWi\nq/GRvaN7MHkwHQYDVR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMB0GA1UdDgQW\nBBQAKsFBtLiVk98qvbGXskbHp+pMUDAfBgNVHSMEGDAWgBRDdYQIzyaQw6t1EiqG\nwliy2PpLCTAYBgNVHREEETAPgg1Tb2Z0VTJGU2lnbmVyMA0GCSqGSIb3DQEBCwUA\nA4IBAQAZyQOPtVHMsjd7Onr86q+84cWyIk0iRnqE9IenJ7WSI+1G/Mv4xAZO0Q8L\n4hmguUjqVTjOQu53PNLqeu4psxBF63rLU1aFENbycutcbP94Tu6+cim1GUYYxQYv\nRVcMZDjOUToUYqql41Y3LA0AGmS/KjWCvz0qFnhiNlTcEt9HDmKlVLfvh8tv4lsB\nURVPX06uxNbt++U/X07z7rNgvnEag/Ah5dEmUa4iBDkR1l+O8C2/FJs4DwKhX2uu\nLBxWUpr8kyxmJ8i+9UH7dALn1bTT9sys3mOFel6z3DNWE1tc7L41hH9oElMhA+QB\nr3b4WgvkosvPpo2+zWr97DvvdynH\n-----END CERTIFICATE-----";
        private const string PrivateKeyPem =
            "-----BEGIN EC PRIVATE KEY-----\nMHcCAQEEIOnAGwHUzAG3Iwyb4pRYhT77IniC8def2BE/Mm09/T7moAoGCCqGSM49\nAwEHoUQDQgAEgr1NR5EjhQTS1B8sqrPmjN0cyPdKX/a2+wbGNGvlcPOBjU/htUop\nEuJmcovu3WaueVQlJ9F9pbmyKVWiq/GRvQ==\n-----END EC PRIVATE KEY-----";
        private const string SignerName = "SHA256/ECDSA";

        private static X509Certificate LoadCertificate()
        {
            var reader = new StringReader(CertificatePem);
            var pem = new PemReader(reader);
            return (X509Certificate)pem.ReadObject();
        }

        private static AsymmetricKeyParameter LoadPrivateKey()
        {
            var reader = new StringReader(PrivateKeyPem);
            var pem = new PemReader(reader);
            var keyPair = (AsymmetricCipherKeyPair) pem.ReadObject();
            return keyPair.Private;
        }

        public static byte[] SignData(byte[] data)
        {
            return SignData(data, LoadPrivateKey());
        }

        public static byte[] SignData(byte[] data, byte[] privateKey)
        {
            return SignData(data, PrivateKeyFactory.CreateKey(privateKey));
        }

        public static byte[] SignData(byte[] data, AsymmetricKeyParameter privateKey)
        {
            var signer = SignerUtilities.GetSigner(SignerName);
            signer.Init(true, privateKey);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }

        public static byte[] GetCertificatePublicKeyInDer()
        {
            var reader = new StringReader(CertificatePem);
            var pem = new PemReader(reader);
            return pem.ReadPemObject().Content;
        }
    }
}
