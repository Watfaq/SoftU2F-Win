using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public class KnownFacets
    {

        private static readonly Dictionary<byte[], string> KnownFacetsMap = new Dictionary<byte[], string>
        {
            { GetDigest("https://github.com/u2f/trusted_facets"), "https://github.com" },
            { GetDigest("https://demo.yubico.com"), "https://demo.yubico.com" },
            { GetDigest("https://www.dropbox.com/u2f-app-id.json"), "https://dropbox.com" },
            { GetDigest("https://www.gstatic.com/securitykey/origins.json"), "https://google.com" },
            { GetDigest("https://vault.bitwarden.com/app-id.json"), "https://vault.bitwarden.com" },
            { GetDigest("https://keepersecurity.com"), "https://keepersecurity.com" },
            { GetDigest("https://api-9dcf9b83.duosecurity.com"), "https://api-9dcf9b83.duosecurity.com" },
            { GetDigest("https://dashboard.stripe.com"), "https://dashboard.stripe.com" },
            { GetDigest("https://id.fedoraproject.org/u2f-origins.json"), "https://id.fedoraproject.org" },
            { GetDigest("https://lastpass.com"), "https://lastpass.com" },

            { Encoding.ASCII.GetBytes("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"), "bogus" }
        };

        public static string GetKnownFacet(byte[] key)
        {
            return KnownFacetsMap.TryGetValue(key, out var facet) ? facet : string.Empty;
        }


        private static byte[] GetDigest(string s)
        {
            using (var sha256Hasher = SHA256.Create())
            {
                return sha256Hasher.ComputeHash(Encoding.UTF8.GetBytes(s));
            }
        }

    }
}
