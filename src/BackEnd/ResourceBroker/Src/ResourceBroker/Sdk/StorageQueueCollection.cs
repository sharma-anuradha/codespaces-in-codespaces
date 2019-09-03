// <copyright file="StorageQueueCollection.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Sdk
{
    /// <summary>
    /// Provides configuration and policy for a Queue DB collection. Ensures that
    /// the configured queue and collection exist.
    /// </summary>
    public abstract class StorageQueueCollection : StorageQueueCollectionBase, IStorageQueueCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageQueueCollection"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public StorageQueueCollection(
            IStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
        }

        /// <summary>
        /// Gets the name that should be logging.
        /// </summary>
        protected abstract string LoggingDocumentName { get; }

        /// <inheritdoc/>
        public async Task AddAsync(string content, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"azurequeue_{LoggingDocumentName}_create",
                async () =>
                {
                    var queue = await GetQueueAsync();
                    var message = new CloudQueueMessage(content);

                    logger.FluentAddValue("QueueVisibilityDelay", initialVisibilityDelay);

                    await queue.AddMessageAsync(message, null, initialVisibilityDelay, null, null);
                });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudQueueMessage>> GetAsync(int popCount, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"azurequeue_{LoggingDocumentName}_get",
                async () =>
                {
                    var queue = await GetQueueAsync();
                    var timeout = TimeSpan.FromMinutes(5);

                    logger.FluentAddValue("QueuePopCount", popCount)
                        .FluentAddValue("QueueVisibilityTimeout", timeout);

                    var results = await queue.GetMessagesAsync(popCount, timeout, null, null);

                    logger.FluentAddValue("QueueFoundItems", results.Count());

                    return results;
                });
        }

        /// <inheritdoc/>
        public async Task<CloudQueueMessage> GetAsync(IDiagnosticsLogger logger)
        {
            return (await GetAsync(1, logger)).FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(CloudQueueMessage message, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"azurequeue_{LoggingDocumentName}_delete",
                async () =>
                {
                    var queue = await GetQueueAsync();

                    await queue.DeleteMessageAsync(message);
                });
        }
    }
}
