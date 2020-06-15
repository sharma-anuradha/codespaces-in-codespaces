// <copyright file="RPSaasMetaRPHttpClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Http
{
    /// <summary>
    /// Used to interact with RPSaaS's MetaRP.
    /// </summary>
    public class RPSaasMetaRPHttpClient : HttpClientBase<RPSaaSMetaRPOptions>, IRPSaaSMetaRPHttpClient
    {
        private const string ResourceProvider = "Microsoft.VSOnline";
        private const string SubscriptionId = "979523fb-a19c-4bb0-a8ee-cef29597b0a4";
        private const string APIVersion = "2020-05-26-preview";

        /// <summary>
        /// Initializes a new instance of the <see cref="RPSaasMetaRPHttpClient"/> class.
        /// </summary>
        /// <param name="httpClientProvider">the HTTP provider.</param>
        public RPSaasMetaRPHttpClient(IHttpClientProvider<RPSaaSMetaRPOptions> httpClientProvider)
            : base(httpClientProvider)
        {
        }

        /// <inheritdoc />
        public async Task<RPRegisteredSubscriptionsRequest> GetSubscriptionDetailsAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            // TODO: This all needs to be filled with a real sub, a real RPSaaS sub and the API version.
            var rpSaaSResponse = await SendAsync<object, RPRegisteredSubscriptionsRequest>(HttpMethod.Get, $"subscriptions/{SubscriptionId}/providers/{ResourceProvider}/registeredSubscriptions/{subscription.Id}?api-version={APIVersion}", null, logger.NewChildLogger());
            return rpSaaSResponse;
        }
    }
}
