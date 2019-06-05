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
            HttpMessageHandler httpHandlerChain = new HttpClientHandler();
            httpHandlerChain = new ForwardingBearerAuthMessageHandler(httpHandlerChain, currentUserProvider);

            var liveShareHttpClient = new System.Net.Http.HttpClient(httpHandlerChain)
            {
                BaseAddress = new Uri(appSettings.VSLiveShareApiEndpoint)
            };
            var computeHttpClient = new System.Net.Http.HttpClient()
            {
                BaseAddress = appSettings.ComputeServiceUri
            };

            ProfileServiceClient = liveShareHttpClient;
            ComputeServiceClient = computeHttpClient;
        }

        public System.Net.Http.HttpClient ProfileServiceClient { get; }

        public System.Net.Http.HttpClient ComputeServiceClient { get; }
    }
}
