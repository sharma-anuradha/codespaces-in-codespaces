﻿// <copyright file="StorageQueueCollection.cs" company="Microsoft">
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

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
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
        /// <param name="resourceNameBuilder">The resource name builder.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public StorageQueueCollection(
            IStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet defaultLogValues)
            : base(clientProvider, healthProvider, loggerFactory, resourceNameBuilder, defaultLogValues)
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
                async (childLogger) =>
                {
                    var queue = await GetQueueAsync();
                    var message = new CloudQueueMessage(content);

                    childLogger.FluentAddValue("QueueVisibilityDelay", initialVisibilityDelay);

                    await queue.AddMessageAsync(message, null, initialVisibilityDelay, null, null);
                });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudQueueMessage>> GetAsync(int popCount, IDiagnosticsLogger logger, TimeSpan? timeout = null)
        {
            return await logger.OperationScopeAsync(
                $"azurequeue_{LoggingDocumentName}_get",
                async (childLogger) =>
                {
                    var queue = await GetQueueAsync();

                    timeout = timeout ?? TimeSpan.FromMinutes(5);

                    childLogger.FluentAddValue("QueuePopCount", popCount)
                        .FluentAddValue("QueueVisibilityTimeout", timeout);

                    var results = await queue.GetMessagesAsync(popCount, timeout, null, null);

                    childLogger.FluentAddValue("QueueFoundItems", results.Count());

                    return results;
                });
        }

        /// <inheritdoc/>
        public async Task<CloudQueueMessage> GetAsync(IDiagnosticsLogger logger, TimeSpan? timeout = null)
        {
            return (await GetAsync(1, logger, timeout)).FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(CloudQueueMessage message, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"azurequeue_{LoggingDocumentName}_delete",
                async (childLogger) =>
                {
                    var queue = await GetQueueAsync();

                    await queue.DeleteMessageAsync(message);
                });
        }

        /// <inheritdoc/>
        public async Task<int?> GetApproximateMessageCount(IDiagnosticsLogger logger)
        {
           return await logger.OperationScopeAsync(
                $"azurequeue_{LoggingDocumentName}_getCount",
                async (childLogger) =>
                {
                    var queue = await GetQueueAsync();
                    await queue.FetchAttributesAsync();
                    return queue.ApproximateMessageCount;
                });
        }
    }
}
