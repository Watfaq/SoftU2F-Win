using System;
using System.Security.Cryptography;

namespace JustTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var data = new byte[]{1,2,3};
            var s = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            Console.WriteLine("encrypted");
            foreach (var b in s)
            {
                Console.Write(b);
                Console.Write(",");
            }

            var ss = ProtectedData.Unprotect(s, null, DataProtectionScope.CurrentUser);
            Console.WriteLine("Unencrypted");
            foreach (var b in ss)
            {
                Console.Write(b);
                Console.Write(",");
            }

            Console.ReadLine();
        }
    }
}
