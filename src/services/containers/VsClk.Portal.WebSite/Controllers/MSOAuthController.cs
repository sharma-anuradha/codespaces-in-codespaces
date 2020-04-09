using Microsoft.AspNetCore.Mvc;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class MSOAuthController : Controller
    {
        private AppSettings AppSettings { get; set; }
        private IDataProtectionProvider Provider { get; set; }

        public MSOAuthController(
            AppSettings appSettings,
            IDataProtectionProvider provider
        )
        {
            AppSettings = appSettings;
            Provider = provider;
        }

        [HttpPost("~/aad-code-grant-v2")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Authorize(
            [FromForm] string client_id,
            [FromForm] string grant_type,
            [FromForm] string redirect_uri,
            [FromForm] string scope,
            [FromForm] string code,
            [FromForm] string refresh_token
        )
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://login.microsoftonline.com/common/oauth2/v2.0/token");
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Dictionary<string, string> data = new Dictionary<string, string>() {
                { "client_id", client_id },
                { "grant_type", grant_type },
                { "scope", scope },
            };

            if (!string.IsNullOrEmpty(code))
            {
                data.Add("code", code);
            }

            if (!string.IsNullOrEmpty(refresh_token))
            {
                data.Add("refresh_token", refresh_token);
            }

            if (!string.IsNullOrEmpty(redirect_uri))
            {
                data.Add("redirect_uri", redirect_uri);
            }

            requestMessage.Content = new FormUrlEncodedContent(data);

            HttpClient client = new HttpClient();

            HttpResponseMessage response = await client.SendAsync(requestMessage);

            var content = await response.Content.ReadAsStringAsync();

            Response.StatusCode = (int)response.StatusCode;

            return Content(content, "application/json");
        }
    }
}
