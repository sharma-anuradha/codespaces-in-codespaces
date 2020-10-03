using Microsoft.VsCloudKernel.Services.Portal.WebSite.Models;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class CookieEncryptionUtils : ICookieEncryptionUtils
    {
        private const char CookieIVSeparator = ':';

        public CookieEncryptionUtils(AppSettings appSettings)
        {
            AppSettings = appSettings;
        }

        private AppSettings AppSettings { get; }

        public PortForwardingAuthCookiePayload DecryptCookie(string encryptedCookie)
        {
            if (string.IsNullOrEmpty(AppSettings.AesKey))
            {
                throw new Exception("AES key is not set.");
            }

            try
            {
                var cookiePair = ParseCascadeCookie(encryptedCookie);
                if (cookiePair == default)
                {
                    return null;
                }

                var decryptedCookie = AesEncryptor.DecryptStringFromHex_Aes(cookiePair.Item1, AppSettings.AesKey, cookiePair.Item2);
                var payload = JsonConvert.DeserializeObject<PortForwardingAuthCookiePayload>(decryptedCookie);

                return payload;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string GetEncryptedCookieContent(string cascadeToken, string environmentId = null, string connectionSessionId = null)
        {
            var cookiePayload = new PortForwardingAuthCookiePayload
            {
                Id = CreateRandomPayload(),
                EnvironmentId = environmentId,
                ConnectionSessionId = connectionSessionId,
                TimeStamp = DateTime.Now.ToLongDateString(),
                CascadeToken = cascadeToken,
            };

            var cookiePayloadString = JsonConvert.SerializeObject(cookiePayload);

            var iv = BuildSecureHexString(32);
            var encryptedCookie = AesEncryptor.EncryptStringToBytes_Aes(cookiePayloadString, AppSettings.AesKey, iv);
            var cookie = $"{iv}{CookieIVSeparator}{encryptedCookie}";

            return cookie;
        }

        public string BuildSecureHexString(int hexCharacters)
        {
            var byteArray = new byte[(int)Math.Ceiling(hexCharacters / 2.0)];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(byteArray);
            }
            return String.Concat(Array.ConvertAll(byteArray, x => x.ToString("X2")));
        }

        private string CreateRandomPayload(int num = 6)
        {
            var str = String.Empty;
            while (num-- > 0)
            {
                str += $"{Guid.NewGuid().ToString()}";
            }

            return str;
        }

        private (string Token, string IV) ParseCascadeCookie(string encryptedCookie)
        {
            var split = encryptedCookie.Split(CookieIVSeparator);

            if (split.Length == 1)
            {
                // the old format with single IV, <token, StandardIV>
                return (split[0], AppSettings.AesIV);
            }

            if (split.Length == 2)
            {
                // the new format with single IV, <token, RandomIV> (IV is the prefix of the string)
                return (split[1], split[0]);
            }

            return default;
        }
    }
}
