using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Clients
{
    public class FrontEndWebApiClient : IFrontEndWebApiClient
    {
        private HttpClient HttpClient { get; }

        public FrontEndWebApiClient(HttpClient httpClient, AppSettings settings)
        {
            HttpClient = httpClient;

            HttpClient.BaseAddress = new Uri(settings.ApiEndpoint);

            var header = new ProductHeaderValue("VSCodespacesPortal", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            var userAgent = new ProductInfoHeaderValue(header);
            HttpClient.DefaultRequestHeaders.UserAgent.Add(userAgent);
        }

        public async Task<CloudEnvironmentResult> GetEnvironmentAsync(string environmentId, string token, IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            var fullRequestUri = new UriBuilder(HttpClient.BaseAddress)
            {
                Path = $"/api/v1/environments/{environmentId}",
            }.Uri;

            var message = new HttpRequestMessage(HttpMethod.Get, fullRequestUri);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // TODO: Add request id and correlation id propagation.
            message.Headers.Add("Accept", "application/json");

            try
            {
                var response = await HttpClient.SendAsync(message);
                logger.AddClientHttpResponseDetails(response);

                await response.ThrowIfFailedAsync();

                var resultBody = await response.Content.ReadAsStringAsync();

                var environment = JsonConvert.DeserializeObject<CloudEnvironmentResult>(resultBody);

                logger.AddDuration(duration);
                logger.LogInfo("frontendwebapiclient_get_environment");

                return environment;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration);
                logger.LogException("frontendwebapiclient_get_environment_failed", ex);
            }

            return null;
        }
    }
}
