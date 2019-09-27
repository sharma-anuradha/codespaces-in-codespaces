using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Formatting;
using System.Threading;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using System;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class AuthUtil
    {
        const string authProvider = "microsoft";

        private static HttpClient client = new HttpClient();

        public static async Task<string> ExchangeToken(string path, string externalToken)
        {
            try
            {
                var response = await client.PostAsync(path, new
                {
                    provider = authProvider,
                    token = externalToken,
                }, new NoCharSetJsonMediaTypeFormatter(), CancellationToken.None);

                var authTokenJson = await response.Content.ReadAsAsync<JObject>();
                var authToken = authTokenJson.Value<string>("access_token");
                return authToken;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        private class NoCharSetJsonMediaTypeFormatter : JsonMediaTypeFormatter
        {
            public override void SetDefaultContentHeaders(
                Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
            {
                base.SetDefaultContentHeaders(type, headers, mediaType);
                headers.ContentType.CharSet = "";
            }
        }
    }
}
