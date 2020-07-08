// <copyright file="RPaasMetaRPHttpClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Http
{
    /// <summary>
    /// Used to interact with RPaaS's MetaRP.
    /// </summary>
    public class RPaasMetaRPHttpClient : HttpClientBase<RPaaSMetaRPOptions>, IRPaaSMetaRPHttpClient
    {
        private const string VSOnlineResourceProvider = "Microsoft.VSOnline";
        private const string CodespacesResourceProvider = "Microsoft.Codespaces";
        private const string SubscriptionId = "979523fb-a19c-4bb0-a8ee-cef29597b0a4";

        // TODO: move api versions to appsettings
        private const string VSOnlineAPIVersion = "2020-05-26-preview";
        private const string CodespacesAPIVersion = "2020-06-16";

        /// <summary>
        /// Initializes a new instance of the <see cref="RPaasMetaRPHttpClient"/> class.
        /// </summary>
        /// <param name="httpClientProvider">the HTTP provider.</param>
        public RPaasMetaRPHttpClient(IHttpClientProvider<RPaaSMetaRPOptions> httpClientProvider)
            : base(httpClientProvider)
        {
        }

        /// <inheritdoc />
        public async Task<RPRegisteredSubscriptionsRequest> GetSubscriptionDetailsAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            string resourceProvider;
            string apiVersion;
            if (subscription.ResourceProvider.Equals(VsoPlanInfo.CodespacesProviderNamespace))
            {
                apiVersion = CodespacesAPIVersion;
                resourceProvider = CodespacesResourceProvider;
            }
            else
            {
                apiVersion = VSOnlineAPIVersion;
                resourceProvider = VSOnlineResourceProvider;
            }

            var endpoint = $"subscriptions/{SubscriptionId}/providers/{resourceProvider}/registeredSubscriptions/{subscription.Id}?api-version={apiVersion}";
            var rpaasResponse = await SendAsync<object, RPRegisteredSubscriptionsRequest>(HttpMethod.Get, endpoint, null, logger.NewChildLogger());
            return rpaasResponse;
        }
    }
}
