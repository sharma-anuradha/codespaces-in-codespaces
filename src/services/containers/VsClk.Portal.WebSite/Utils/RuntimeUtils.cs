using System;
using System.Security.Cryptography;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils {
    public static class RuntimeUtils {
        public static string GetNonceBase64(int byteSize = 20)
        {
            var byteArray = GetNonceBytes(byteSize);
            // Base64 encode and then return
            return Convert.ToBase64String(byteArray);
        }

        public static byte[] GetNonceBytes(int byteSize = 20)
        {
            // Allocate a buffer
            var byteArray = new byte[byteSize];
            // Generate a cryptographically random set of bytes
            using (var Rnd = RandomNumberGenerator.Create())
            {
                Rnd.GetBytes(byteArray);
            }

            return byteArray;
        }
    }
}