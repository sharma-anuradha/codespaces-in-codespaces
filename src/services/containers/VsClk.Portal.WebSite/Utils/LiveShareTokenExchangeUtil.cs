using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class LiveShareTokenExchangeUtil : ILiveShareTokenExchangeUtil
    {
        const string authProvider = "microsoft";

        private static HttpClient client = new HttpClient();

        public async Task<string> ExchangeToken(string path, string externalToken)
        {
            try
            {
                var payload = JsonConvert.SerializeObject(new
                {
                    provider = authProvider,
                    token = externalToken,
                });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                // The auth service is implemented in NODE.JS and doesn't like the charset content type to be set.
                // (From the liveshare agent code and documentation.)
                content.Headers.ContentType.CharSet = string.Empty;

                var response = await client.PostAsync(path, content);
                var authTokenJson = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(authTokenJson);
                var authToken = json.Value<string>("access_token");
                return authToken;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
