// <copyright file="CrossRegionStorageQueueCollectionBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Provides configuration and policy for a Queue DB collection. Ensures that
    /// the configured queue and collection exist.
    /// </summary>
    public abstract class CrossRegionStorageQueueCollectionBase : IAsyncWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossRegionStorageQueueCollectionBase"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="controlPlaneInfo">Control plane info.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="resourceNameBuilder">The resource name builder.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CrossRegionStorageQueueCollectionBase(
            ICrossRegionStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IControlPlaneInfo controlPlaneInfo,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet defaultLogValues)
        {
            Requires.NotNull(clientProvider, nameof(clientProvider));
            Requires.NotNull(healthProvider, nameof(healthProvider));
            Requires.NotNull(loggerFactory, nameof(loggerFactory));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));

            var logger = loggerFactory.New(defaultLogValues);

            // Start initialization in the background.
            // Invoking GetClientAsync will ensure initialization is complete.
            InitializeQueueTask = InitializeQueue(clientProvider, healthProvider, controlPlaneInfo, logger);
        }

        /// <summary>
        /// Gets the collection id for this collection.
        /// </summary>
        protected abstract string QueueId { get; }

        /// <summary>
        /// Gets the resource name builder.
        /// </summary>
        protected IResourceNameBuilder ResourceNameBuilder { get; }

        private Task<ReadOnlyDictionary<AzureLocation, CloudQueue>> InitializeQueueTask { get; set; }

        /// <inheritdoc/>
        async Task IAsyncWarmup.WarmupCompletedAsync()
        {
            await InitializeQueueTask;
        }

        /// <summary>
        /// Gets the fully initialized <see cref="DocumentClient"/> instance for the given control plane region.
        /// </summary>
        /// <param name="controlPlaneRegion">Control plane region.</param>
        /// <returns>A task whose result is the document client.</returns>
        protected async Task<CloudQueue> GetQueueAsync(AzureLocation controlPlaneRegion)
        {
            var queueClients = await InitializeQueueTask;
            return queueClients[controlPlaneRegion];
        }

        private Task<ReadOnlyDictionary<AzureLocation, CloudQueue>> InitializeQueue(
            ICrossRegionStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IControlPlaneInfo controlPlaneInfo,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "queue_initialization",
                async (childLogger) =>
                {
                    var queueClients = new Dictionary<AzureLocation, CloudQueue>();
                    foreach (var controlPlaneStampInfo in controlPlaneInfo.AllStamps.Values.Distinct())
                    {
                        var controlPlaneRegion = controlPlaneInfo.Stamp.Location;
                        var queueClient = await clientProvider.GetQueueAsync(QueueId, controlPlaneRegion);
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
