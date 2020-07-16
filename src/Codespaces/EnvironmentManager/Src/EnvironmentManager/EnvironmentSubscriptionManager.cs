// <copyright file="EnvironmentSubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment Subscription Manager.
    /// </summary>
    public class EnvironmentSubscriptionManager : IEnvironmentSubscriptionManager
    {
        private const string LogBaseName = "environment_subscription_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSubscriptionManager"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="skuCatalog">Target Sku Catalog.</param>
        public EnvironmentSubscriptionManager(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ISkuCatalog skuCatalog)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private ISkuCatalog SkuCatalog { get; }

        /// <inheritdoc/>
        public async Task<bool> HasReachedMaxComputeUsedForSubscriptionAsync(
            Subscription subscription,
            ICloudEnvironmentSku desiredSku,
            IDiagnosticsLogger logger)
        {
            var currentComputeUsed = await GetCurrentComputeUsedForSubscriptionAsync(subscription, desiredSku, logger);
            int currentMaxQuota = subscription.CurrentMaximumQuota[desiredSku.ComputeSkuFamily];

            bool hasMaxComputeUsed = currentComputeUsed + desiredSku.ComputeSkuCores > currentMaxQuota;
            if (hasMaxComputeUsed)
            {
                logger.AddValue("RequestedSku", desiredSku.SkuName);
                logger.AddValue("CurrentMaxQuota", currentMaxQuota.ToString());
                logger.AddValue("CurrentComputeUsed", currentComputeUsed.ToString());
                logger.AddSubscriptionId(subscription.Id);
                logger.LogError($"{LogBaseName}_create_exceed_compute_quota");
            }

            return hasMaxComputeUsed;
        }

        /// <inheritdoc/>
        public async Task<int> GetCurrentComputeUsedForSubscriptionAsync(
            Subscription subscription,
            ICloudEnvironmentSku desiredSku,
            IDiagnosticsLogger logger)
        {
            var allEnvs = await CloudEnvironmentRepository.GetAllEnvironmentsInSubscriptionAsync(subscription.Id, logger);
            var computeUsed = 0;
            foreach (var env in allEnvs)
            {
                if (IsEnvironmentInComputeUtilizingState(env))
                {
                    var sku = GetSku(env);
                    if (sku.ComputeSkuFamily.Equals(desiredSku.ComputeSkuFamily, StringComparison.OrdinalIgnoreCase))
                    {
                        computeUsed += sku.ComputeSkuCores;
                    }
                }
            }

            return computeUsed;
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> ListBySubscriptionAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            Requires.NotNull(logger, nameof(logger));
            Requires.NotNull(logger, nameof(subscription));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_list_by_subscription",
                async (childLogger) =>
                {
                    return await CloudEnvironmentRepository.GetAllEnvironmentsInSubscriptionAsync(subscription.Id, logger.NewChildLogger());
                });
        }

        private bool IsEnvironmentInComputeUtilizingState(CloudEnvironment cloudEnvironment)
        {
            switch (cloudEnvironment.State)
            {
                case CloudEnvironmentState.None:
                case CloudEnvironmentState.Created:
                case CloudEnvironmentState.Queued:
                case CloudEnvironmentState.Provisioning:
                case CloudEnvironmentState.Available:
                case CloudEnvironmentState.Awaiting:
                case CloudEnvironmentState.Unavailable:
                case CloudEnvironmentState.Starting:
                case CloudEnvironmentState.ShuttingDown:
                    return true;
                case CloudEnvironmentState.Deleted:
                case CloudEnvironmentState.Shutdown:
                case CloudEnvironmentState.Archived:
                case CloudEnvironmentState.Failed:
                    return false;
                default:
                    return true;
            }
        }

        private ICloudEnvironmentSku GetSku(CloudEnvironment cloudEnvironment)
        {
            if (!SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var sku))
            {
                throw new ArgumentException($"Invalid SKU: {cloudEnvironment.SkuName}");
            }

            return sku;
        }
    }
}
