using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class LiveShareTokenExchangeUtil : ILiveShareTokenExchangeUtil
    {
        private readonly HttpClient httpClient;
        private readonly AppSettings appSettings;

        private const string AuthProvider = "microsoft";

        public LiveShareTokenExchangeUtil(HttpClient httpClient, AppSettings appSettings)
        {
            this.httpClient = httpClient;
            this.appSettings = appSettings;
        }

        public async Task<string> ExchangeTokenAsync(string externalToken)
        {
            var fullRequestUri = new UriBuilder(appSettings.LiveShareEndpoint)
            {
                Path = Constants.LiveShareTokenExchangeRoute,
            }.Uri;

            try
            {
                var payload = JsonConvert.SerializeObject(new
                {
                    provider = AuthProvider,
                    token = externalToken,
                });
                var content = new StringContent(payload, Encoding.UTF8, MediaTypeNames.Application.Json);

                // The auth service is implemented in NODE.JS and doesn't like the charset content type to be set.
                // (From the liveshare agent code and documentation.)
                content.Headers.ContentType.CharSet = string.Empty;

                var response = await httpClient.PostAsync(fullRequestUri, content);
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
