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
            "-----BEGIN CERTIFICATE-----\nMIIBfjCCASSgAwIBAgIBATAKBggqhkjOPQQDAjA8MREwDwYDVQQDDAhTb2Z0IFUyRjEUMBIGA1UECgwLR2l0SHViIEluYy4xETAPBgNVBAsMCFNlY3VyaXR5MB4XDTE3MDcyNjIwMDkwOFoXDTI3MDcyNDIwMDkwOFowPDERMA8GA1UEAwwIU29mdCBVMkYxFDASBgNVBAoMC0dpdEh1YiBJbmMuMREwDwYDVQQLDAhTZWN1cml0eTBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABPacqyQUS7Tvh/cPIxxc1PV4BKz44Mays+NSGD2AOR9r0nnSakyDZHTmwtojk/+sHVA0bFwjkGVXkz7Lk/9u3tGjFzAVMBMGCysGAQQBguUcAgEBBAQDAgMIMAoGCCqGSM49BAMCA0gAMEUCIQD+Ih2XuOrqErufQhSFD0gXZbXglZNeoaPWbQ+xbzn3IgIgZNfcL1xsOCr3ZfV4ajmwsUqXRSjvfd8hAhUbiErUQXo=\n-----END CERTIFICATE-----";
        private const string PrivateKeyPem =
            "-----BEGIN EC PRIVATE KEY-----\nMHcCAQEEIAOEKsf0zeNn3qBWxk9/OxXqfUvEg8rGl58qMZOtVzEJoAoGCCqGSM49AwEHoUQDQgAE9pyrJBRLtO+H9w8jHFzU9XgErPjgxrKz41IYPYA5H2vSedJqTINkdObC2iOT/6wdUDRsXCOQZVeTPsuT/27e0Q==\n-----END EC PRIVATE KEY-----";
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
