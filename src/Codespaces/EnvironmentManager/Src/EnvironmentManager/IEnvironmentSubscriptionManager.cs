// <copyright file="IEnvironmentSubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment Subscription Manager.
    /// </summary>
    public interface IEnvironmentSubscriptionManager
    {
        /// <summary>
        /// Determine if subscription has reached compute limit.
        /// </summary>
        /// <param name="subscription">Target Subscription.</param>
        /// <param name="desiredSku">Target Desired Sku.</param>
        /// <param name="logger">Target Logger.</param>
        /// <returns>Returns if limit is hit.</returns>
        Task<SubscriptionComputeData> HasReachedMaxComputeUsedForSubscriptionAsync(
            Subscription subscription,
            ICloudEnvironmentSku desiredSku,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Total current compute used for a subscription.
        /// </summary>
        /// <param name="subscription">Target Subscription.</param>
        /// <param name="desiredSku">Target Desired Sku.</param>
        /// <param name="logger">Target Logger.</param>
        /// <returns>Returns total count.</returns>
        Task<int> GetCurrentComputeUsedForSubscriptionAsync(
            Subscription subscription,
            ICloudEnvironmentSku desiredSku,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all environments under the given Azure subscription.
        /// </summary>
        /// <param name="subscription">Required Azure subscription.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the list of <see cref="CloudEnvironment"/>.</returns>
        Task<IEnumerable<CloudEnvironment>> ListBySubscriptionAsync(
            Subscription subscription,
            IDiagnosticsLogger logger);
    }
}
