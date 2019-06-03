using System;
using System.Net.Http;
using Microsoft.VsCloudKernel.Services.EnvReg.Models;

namespace VsClk.EnvReg.Repositories.Support.HttpClient
{
    public class HttpClientProvider : IHttpClientProvider
    {
        public HttpClientProvider(
            ICurrentUserProvider currentUserProvider,
            AppSettings appSettings)
        {
            var serviceBaseVersionAddress = new Uri(appSettings.VSLiveShareApiEndpoint);

            HttpMessageHandler httpHandlerChain = new HttpClientHandler();
            httpHandlerChain = new ForwardingBearerAuthMessageHandler(httpHandlerChain, currentUserProvider);

            var liveShareHttpClient = new System.Net.Http.HttpClient(httpHandlerChain)
            {
                BaseAddress = serviceBaseVersionAddress
            };

            ProfileServiceClient = liveShareHttpClient;
        }

        public System.Net.Http.HttpClient ProfileServiceClient { get; }
    }
}
