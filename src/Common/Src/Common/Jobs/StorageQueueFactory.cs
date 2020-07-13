// <copyright file="StorageQueueFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// A queue factory based on an Azure Storage Queue.
    /// </summary>
    public class StorageQueueFactory : IQueueFactory
    {
        private readonly ConcurrentDictionary<string, StorageQueue> storageQueues = new ConcurrentDictionary<string, StorageQueue>();
        private readonly Func<string, StorageQueue> createCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageQueueFactory"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="resourceNameBuilder">The resource name builder.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public StorageQueueFactory(
            IStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet defaultLogValues)
        {
            this.createCallback = (queueId) => new StorageQueue(this, queueId, clientProvider, healthProvider, loggerFactory, resourceNameBuilder, defaultLogValues);
        }

        /// <inheritdoc/>
        public IQueue GetOrCreate(string queueId)
        {
            Requires.NotNullOrEmpty(queueId, nameof(queueId));
            return this.storageQueues.GetOrAdd(queueId, (id) => this.createCallback(queueId));
        }

        private class StorageQueue : StorageQueueCollectionBase, IQueue, IAsyncDisposable
        {
            private const string LoggingPrefix = "azurequeue_storage_queue";

            private readonly Func<IDiagnosticsLogger> loggerFactoryCallback;

            public StorageQueue(
                IQueueFactory queueFactory,
                string queueId,
                IStorageQueueClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                IResourceNameBuilder resourceNameBuilder,
                LogValueSet defaultLogValues)
            : base(clientProvider, healthProvider, loggerFactory, resourceNameBuilder, defaultLogValues, () => resourceNameBuilder.GetQueueName(queueId))
            {
                Factory = queueFactory;
                this.loggerFactoryCallback = () => loggerFactory.New(defaultLogValues);
            }

            public IQueueFactory Factory { get; }

            protected override string QueueId => throw new NotImplementedException();

            public async ValueTask DisposeAsync()
            {
                await CreateLogger().OperationScopeAsync(
                    $"{LoggingPrefix}_dispose",
                    async (childLogger) =>
                    {
                        var queue = await GetQueueAsync();
                        await queue.DeleteAsync();
                    });
            }

            /// <inheritdoc/>
            public Task<QueueMessage> AddMessageAsync(byte[] content, TimeSpan? initialVisibilityDelay, CancellationToken cancellationToken)
            {
                return CreateLogger().OperationScopeAsync(
                    $"{LoggingPrefix}_add",
                    async (childLogger) =>
                    {
                        var queue = await GetQueueAsync();
                        var message = new CloudQueueMessage(content);

                        await queue.AddMessageAsync(message, null, initialVisibilityDelay, null, null, cancellationToken);
                        return new QueueMessageAdapter(message) as QueueMessage;
                    });
            }

            /// <inheritdoc/>
            public Task<IEnumerable<QueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, TimeSpan timeout, CancellationToken cancellationToken)
            {
                return CreateLogger().OperationScopeAsync(
                    $"{LoggingPrefix}_get",
                    async (childLogger) =>
                    {
                        var queue = await GetQueueAsync();

                        childLogger.FluentAddValue("MessageCount", messageCount)
                            .FluentAddValue("VisibilityTimeout", visibilityTimeout);

                        var results = await queue.GetMessagesAsync(messageCount, visibilityTimeout, null, null, cancellationToken);

                        if (!results.Any() && timeout != TimeSpan.Zero)
                        {
                            await Task.Delay(timeout, cancellationToken);
                            results = await queue.GetMessagesAsync(messageCount, visibilityTimeout, null, null, cancellationToken);
                        }

                        if (results.Any() && visibilityTimeout == null)
                        {
                            foreach (var message in results)
                            {
                                await queue.DeleteMessageAsync(message, cancellationToken);
                            }
                        }

                        childLogger.FluentAddValue("QueueFoundItems", results.Count());
                        return results.Select(m => new QueueMessageAdapter(m) as QueueMessage);
                    });
            }

            /// <inheritdoc/>
            public Task DeleteMessageAsync(QueueMessage queueMessage, CancellationToken cancellationToken)
            {
                return CreateLogger().OperationScopeAsync(
                    $"{LoggingPrefix}_delete",
                    async (childLogger) =>
                    {
                        var queue = await GetQueueAsync();
                        await queue.DeleteMessageAsync(QueueMessageAdapter.AsCloudQueueMessage(queueMessage), cancellationToken);
                    });
            }

            /// <inheritdoc/>
            public Task UpdateMessageAsync(QueueMessage queueMessage, bool updateContent, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
            {
                return CreateLogger().OperationScopeAsync(
                    $"{LoggingPrefix}_update",
                    async (childLogger) =>
                    {
                        var queue = await GetQueueAsync();
                        MessageUpdateFields messageUpdateFields =
                            MessageUpdateFields.Visibility | (updateContent ? MessageUpdateFields.Content : 0);

                        var cloudMessage = QueueMessageAdapter.AsCloudQueueMessage(queueMessage);
                        await queue.UpdateMessageAsync(
                            cloudMessage,
                            visibilityTimeout,
                            messageUpdateFields);
                    });
            }

            private IDiagnosticsLogger CreateLogger() => this.loggerFactoryCallback();
        }

        private class QueueMessageAdapter : QueueMessage
        {
            public QueueMessageAdapter(CloudQueueMessage cloudQueueMessage)
            {
                CloudQueueMessage = cloudQueueMessage;
            }

            public CloudQueueMessage CloudQueueMessage { get; }

            public override string Id => CloudQueueMessage.Id;

            public override byte[] Content
            {
                get
                {
                    return CloudQueueMessage.AsBytes;
                }

                set
                {
                    CloudQueueMessage.SetMessageContent2(value);
                }
            }

            public static CloudQueueMessage AsCloudQueueMessage(QueueMessage queueMessage)
            {
                return ((QueueMessageAdapter)queueMessage).CloudQueueMessage;
            }
        }
    }
}
