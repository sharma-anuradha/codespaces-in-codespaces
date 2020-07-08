// <copyright file="IRPaaSMetaRPHttpClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Http
{
    /// <summary>
    /// Interface for interacting with RPaas's MetaRP.
    /// </summary>
    public interface IRPaaSMetaRPHttpClient
    {
        /// <summary>
        /// Gets the RPaaS MetaRP details about a specific subscription.
        /// </summary>
        /// <param name="subscription">The subscription the details are sought from.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>subscription details.</returns>
        Task<RPRegisteredSubscriptionsRequest> GetSubscriptionDetailsAsync(Subscription subscription, IDiagnosticsLogger logger);
    }
}
