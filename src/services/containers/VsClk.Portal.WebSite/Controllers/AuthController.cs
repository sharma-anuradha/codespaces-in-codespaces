using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using System.Security.Cryptography;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class AuthController : Controller
    {
        private static AppSettings AppSettings { get; set; }
        private static readonly char CookieIVSeparator = ':';

        public AuthController(
            AppSettings appSettings)
        {
            AppSettings = appSettings;
        }

        public class AuthPFPayload
        {
            public string accessToken { get; set; }
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

        public static string BuildSecureHexString(int hexCharacters)
        {
            var byteArray = new byte[(int)Math.Ceiling(hexCharacters / 2.0)];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(byteArray);
            }
            return String.Concat(Array.ConvertAll(byteArray, x => x.ToString("X2")));
        }

        private void SetCascadeCookie(string cascadeToken)
        {
            var cookiePayload = new CookiePayload
            {
                Id = CreateRandomPayload(),
                TimeStamp = DateTime.Now.ToLongDateString(),
                CascadeToken = cascadeToken,
            };

            var cookiePayloadString = JsonConvert.SerializeObject(cookiePayload);

            var iv = BuildSecureHexString(32);
            var encryptedCookie = AesEncryptor.EncryptStringToBytes_Aes(cookiePayloadString, AppSettings.AesKey, iv);

            CookieOptions option = CreateCookieOptions();
            option.Expires = DateTime.Now.AddDays(Constants.PortForwarderCookieExpirationDays);
            option.SameSite = SameSiteMode.Lax;

            var cookie = $"{iv}{CookieIVSeparator}{encryptedCookie}";
            Response.Cookies.Append(Constants.PFCookieName, cookie, option);
        }

        public static Tuple<string, string> ParseCascadeCookie(string encryptedCookie)
        {
            var split = encryptedCookie.Split(CookieIVSeparator);

            if (split.Length == 1)
            {
                // the old format with single IV, <token, StandardIV>
                return new Tuple<string, string>(split[0], AppSettings.AesIV);
            }

            if (split.Length == 2)
            {
                // the new format with single IV, <token, RandomIV> (IV is the prefix of the string)
                return new Tuple<string, string>(split[1], split[0]);
            }

            return null;
        }

        [HttpPost("~/authenticate-port-forwarder")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> AuthenticatePortForwarderAsync(
            [FromForm] string token,
            [FromForm] string cascadeToken
        )
        {   
            if (string.IsNullOrWhiteSpace(token) && string.IsNullOrWhiteSpace(cascadeToken))
            {
                return BadRequest();
            }

            if (!string.IsNullOrEmpty(token))
            {
                cascadeToken = await AuthUtil.ExchangeToken(AppSettings.LiveShareEndpoint + Constants.LiveShareTokenExchangeRoute, token);
            }

            SetCascadeCookie(cascadeToken);
            return Ok(200);
        }

        [HttpPost("~/logout-port-forwarder")]
        public IActionResult LogoutPortForwarder()
        {
            CookieOptions option = CreateCookieOptions();
            option.Expires = DateTime.Now.AddDays(-100);

            Response.Cookies.Append(Constants.PFCookieName, string.Empty, option);
            
            return Ok(200);
        }

        public static CookiePayload DecryptCookie(string encryptedCookie)
        {
            try
            {
                var cookiePair = ParseCascadeCookie(encryptedCookie);
                if (cookiePair == null)
                {
                    return null;
                }

                var decryptedCookie = AesEncryptor.DecryptStringFromHex_Aes(cookiePair.Item1, AppSettings.AesKey, cookiePair.Item2);
                var payload = JsonConvert.DeserializeObject<CookiePayload>(decryptedCookie);

                return payload;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public class CookiePayload
        {
            [JsonProperty("timeStamp")]
            public string TimeStamp { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }
            /*Properties will be decrypted in order, better to have token as the last one for security purpose.
            **by default "order = -1" for JSON properties, wee need to make it "1" to be the last one.
            */
            [JsonProperty(PropertyName = "cascadeToken", Order = 1)]
            public string CascadeToken { get; set; }
        }

        private CookieOptions CreateCookieOptions()
        {
            CookieOptions option = new CookieOptions
            {
                Path = "/",
                Domain = $"{AppSettings.Domain}",
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            };

            return option;
        }
    }
}
