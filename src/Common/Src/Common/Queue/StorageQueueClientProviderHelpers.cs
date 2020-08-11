// <copyright file="StorageQueueClientProviderHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Helpers for the storage queue client providers.
    /// </summary>
    internal static class StorageQueueClientProviderHelpers
    {
        /// <summary>
        /// Initialize a cloud queue.
        /// </summary>
        /// <param name="clientProvider">The client provider instance.</param>
        /// <param name="queueId">Queue id.</param>
        /// <param name="healthProvider">A health provider.</param>
        /// <param name="logger">Logger instance.</param>
        /// <returns>Task completion.</returns>
        public static async Task<CloudQueue> InitializeQueue(
            this IStorageQueueClientProvider clientProvider,
            string queueId,
            IHealthProvider healthProvider,
            IDiagnosticsLogger logger)
        {
            var initializationDuration = logger.StartDuration();
            try
            {
                var client = await clientProvider.GetQueueAsync(queueId);

                logger.AddDuration(initializationDuration)
                    .LogInfo("queue_initialization_success");

                return client;
            }
            catch (Exception e)
            {
                logger.AddDuration(initializationDuration)
                    .LogException($"queue_initalization_error", e);

                // We cannot use the service at this point. Mark it as unhealthy to request a restart.
                healthProvider.MarkUnhealthy(e, logger);

                throw;
            }
        }

        /// <summary>
        /// Initialize a cross region queue.
        /// </summary>
        /// <param name="clientProvider">The cross region client provider instance.</param>
        /// <param name="queueId">Queue id.</param>
        /// <param name="healthProvider">A health provider.</param>
        /// <param name="controlPlaneInfo">Control plane info. </param>
        /// <param name="logger">Logger instance.</param>
        /// <returns>Task completion.</returns>
        public static Task<ReadOnlyDictionary<AzureLocation, CloudQueue>> InitializeQueue(
            this ICrossRegionStorageQueueClientProvider clientProvider,
            string queueId,
            IHealthProvider healthProvider,
            IControlPlaneInfo controlPlaneInfo,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "queue_initialization",
                async (childLogger) =>
                {
                    var queueClients = new Dictionary<AzureLocation, CloudQueue>();
                    foreach (var controlPlaneRegion in controlPlaneInfo.AllStamps.Keys)
                    {
                        var queueClient = await clientProvider.GetQueueAsync(queueId, controlPlaneRegion);
                        queueClients.Add(controlPlaneRegion, queueClient);
                    }

                    return new ReadOnlyDictionary<AzureLocation, CloudQueue>(queueClients);
                },
                (e, childLogger) =>
                {
                    // We cannot use the service at this point. Mark it as unhealthy to request a restart.
                    healthProvider.MarkUnhealthy(e, logger);

                    return Task.FromResult(default(ReadOnlyDictionary<AzureLocation, CloudQueue>));
                });
        }
    }
}
