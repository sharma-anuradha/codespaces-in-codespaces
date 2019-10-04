using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class AuthController : Controller
    {
        private static AppSettings AppSettings { get; set; }

        public AuthController(
            AppSettings appSettings)
        {
            AppSettings = appSettings;
        }

        public class AuthPFPayload
        {
            public string accessToken { get; set; }
        }

        [HttpPost("~/authenticate-port-forwarder")]
        public async Task<IActionResult> AuthenticatePortForwarder([FromBody] AuthPFPayload bodyPayload)
        {
            if (string.IsNullOrEmpty(AppSettings.AesKey)
                 || string.IsNullOrEmpty(AppSettings.AesIV)
                 || string.IsNullOrEmpty(AppSettings.Domain)
             )
            {
                return BadRequest("AesKey, AesIV or Domain keys are not found in the app settings.");
            }

            if (
               string.IsNullOrWhiteSpace(bodyPayload.accessToken)
            )
            {
                return BadRequest();
            }

            var cascadeToken = await AuthUtil.ExchangeToken(AppSettings.LiveShareEndpoint + Constants.LiveShareTokenExchangeRoute, bodyPayload.accessToken);
            var cookiePayload = new CookiePayload
            {
                CascadeToken = cascadeToken,
                Id = Guid.NewGuid().ToString(),
                TimeStamp = DateTime.Now.ToLongDateString()
            };

            var cookiePayloadString = JsonConvert.SerializeObject(cookiePayload);
            var encryptedCookie = AesEncryptor.EncryptStringToBytes_Aes(cookiePayloadString, AppSettings.AesKey, AppSettings.AesIV);

            CookieOptions option = new CookieOptions();

            option.Path = "/";
            option.Domain = $"{AppSettings.Domain}";
            option.HttpOnly = true;
            option.Secure = true;
            option.SameSite = SameSiteMode.Lax;
            option.Expires = DateTime.Now.AddDays(Constants.PortForwarderCookieExpirationDays);

            Response.Cookies.Append(Constants.PFCookieName, encryptedCookie, option);

            return Ok();
        }

        [HttpPost("~/signout-port-forwarder")]
        public async Task<IActionResult> SignOutPortForwarder()
        {
            Response.Cookies.Delete(Constants.PFCookieName);

            return Ok();
        }

        public static CookiePayload DecryptCookie(string encryptedCookie)
        {
            try
            {
                var decryptedCookie = AesEncryptor.DecryptStringFromHex_Aes(encryptedCookie, AppSettings.AesKey, AppSettings.AesIV);
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
            /*Properties will be decrypted in order, better to have token as the last one for security purpose.
            **by default "order = -1" for JSON properties, wee need to make it "1" to be the last one.
            */
            [JsonProperty(PropertyName = "cascadeToken", Order = 1)]
            public string CascadeToken { get; set; }

            [JsonProperty("timeStamp")]
            public string TimeStamp { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }
        }
    }
}
