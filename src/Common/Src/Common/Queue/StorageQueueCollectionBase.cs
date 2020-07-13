﻿// <copyright file="StorageQueueCollectionBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Provides configuration and policy for a Queue DB collection. Ensures that
    /// the configured queue and collection exist.
    /// </summary>
    public abstract class StorageQueueCollectionBase : IAsyncWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageQueueCollectionBase"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="resourceNameBuilder">The resource name builder.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        /// <param name="queueIdCallback">Callback to evaulate for the queueId.</param>
        public StorageQueueCollectionBase(
            IStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet defaultLogValues,
            Func<string> queueIdCallback = null)
        {
            Requires.NotNull(clientProvider, nameof(clientProvider));
            Requires.NotNull(healthProvider, nameof(healthProvider));
            Requires.NotNull(loggerFactory, nameof(loggerFactory));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));

            var logger = loggerFactory.New(defaultLogValues);

            // Start initialization in the background.
            // Invoking GetClientAsync will ensure initialization is complete.
            InitializeQueueTask = InitializeQueue(queueIdCallback != null ? queueIdCallback() : QueueId, clientProvider, healthProvider, logger);
        }

        /// <summary>
        /// Gets the collection id for this collection.
        /// </summary>
        protected abstract string QueueId { get; }

        /// <summary>
        /// Gets the resource name builder.
        /// </summary>
        protected IResourceNameBuilder ResourceNameBuilder { get;  }

        private Task<CloudQueue> InitializeQueueTask { get; }

        /// <inheritdoc/>
        async Task IAsyncWarmup.WarmupCompletedAsync()
        {
            await InitializeQueueTask;
        }

        /// <summary>
        /// Gets the fully initialized <see cref="DocumentClient"/> instance.
        /// </summary>
        /// <returns>A task whose result is the document client.</returns>
        protected async Task<CloudQueue> GetQueueAsync()
        {
            return await InitializeQueueTask;
        }

        private static async Task<CloudQueue> InitializeQueue(
            string queueId,
            IStorageQueueClientProvider clientProvider,
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
    }
}
