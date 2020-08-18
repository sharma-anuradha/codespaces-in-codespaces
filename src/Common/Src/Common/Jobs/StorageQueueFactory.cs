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
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// A queue factory based on an Azure Storage Queue.
    /// </summary>
    public class StorageQueueFactory : IQueueFactory
    {
        private readonly ConcurrentDictionary<(string, AzureLocation?), StorageQueue> storageQueues = new ConcurrentDictionary<(string, AzureLocation?), StorageQueue>();
        private readonly Func<string, StorageQueue> createCallback;
        private readonly Func<string, AzureLocation, StorageQueue> createRegionCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageQueueFactory"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="crossRegionClientProvider">The cross region client provider.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="resourceNameBuilder">Resource name builder instance.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="logger">The logger instance.</param>
        public StorageQueueFactory(
            IStorageQueueClientProvider clientProvider,
            ICrossRegionStorageQueueClientProvider crossRegionClientProvider,
            IControlPlaneInfo controlPlaneInfo,
            IResourceNameBuilder resourceNameBuilder,
            IHealthProvider healthProvider,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            Requires.NotNull(healthProvider, nameof(healthProvider));

            this.createCallback = (queueId) =>
                new StorageQueue(
                    this,
                    () => clientProvider.InitializeQueue(resourceNameBuilder.GetQueueName(queueId), healthProvider, logger),
                    logger);
            this.createRegionCallback = (queueId, controlPlaneRegion) =>
            {
                var initializeQueueTask = crossRegionClientProvider.InitializeQueue(resourceNameBuilder.GetQueueName(queueId), healthProvider, controlPlaneInfo, logger);
                return new StorageQueue(
                    this,
                    async () =>
                    {
                        var queueClients = await initializeQueueTask;
                        return queueClients[controlPlaneRegion];
                    },
                    logger);
            };
        }

        /// <inheritdoc/>
        public IQueue GetOrCreate(string queueId, AzureLocation? azureLocation)
        {
            Requires.NotNullOrEmpty(queueId, nameof(queueId));
            return this.storageQueues.GetOrAdd((queueId, azureLocation), (id) => azureLocation.HasValue ? this.createRegionCallback(queueId, azureLocation.Value) : this.createCallback(queueId));
        }

        private class StorageQueue : IQueue, IAsyncDisposable
        {
            private const string LoggingPrefix = "azurequeue_storage_queue";

            private readonly Func<Task<CloudQueue>> cloudQueueFactoryCallback;
            private IDiagnosticsLogger logger;

            public StorageQueue(
                IQueueFactory queueFactory,
                Func<Task<CloudQueue>> cloudQueueFactoryCallback,
                IDiagnosticsLogger logger)
            {
                Factory = queueFactory;
                this.cloudQueueFactoryCallback = cloudQueueFactoryCallback;
                this.logger = logger;
            }

            public IQueueFactory Factory { get; }

            public async ValueTask DisposeAsync()
            {
                await this.logger.OperationScopeAsync(
                    $"{LoggingPrefix}_dispose",
                    async (childLogger) =>
                    {
                        var queue = await this.cloudQueueFactoryCallback();
                        await queue.DeleteAsync();
                    });
            }

            /// <inheritdoc/>
            public Task<QueueMessage> AddMessageAsync(byte[] content, TimeSpan? initialVisibilityDelay, CancellationToken cancellationToken)
            {
                return this.logger.OperationScopeAsync(
                    $"{LoggingPrefix}_add",
                    async (childLogger) =>
                    {
                        var queue = await this.cloudQueueFactoryCallback();
                        var message = new CloudQueueMessage(content);

                        await queue.AddMessageAsync(message, null, initialVisibilityDelay, null, null, cancellationToken);
                        return new QueueMessageAdapter(message) as QueueMessage;
                    });
            }

            /// <inheritdoc/>
            public Task<IEnumerable<QueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, TimeSpan timeout, CancellationToken cancellationToken)
            {
                return this.logger.OperationScopeAsync(
                    $"{LoggingPrefix}_get",
                    async (childLogger) =>
                    {
                        var queue = await this.cloudQueueFactoryCallback();

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
                        return results.Select(m =>
                        {
                            this.logger.NewChildLogger().FluentAddValue(JobQueueLoggerConst.JobId, m.Id)
                                .FluentAddValue("DequeueCount", m.DequeueCount)
                                .FluentAddValue("NextVisibleTime", m.NextVisibleTime)
                                .FluentAddValue("ExpirationTime", m.ExpirationTime)
                                .FluentAddValue("InsertionTime", m.InsertionTime)
                                .LogInfo($"{LoggingPrefix}_get_message_complete");

                            return new QueueMessageAdapter(m) as QueueMessage;
                        }).ToArray() as IEnumerable<QueueMessage>;
                    });
            }

            /// <inheritdoc/>
            public Task DeleteMessageAsync(QueueMessage queueMessage, CancellationToken cancellationToken)
            {
                return this.logger.OperationScopeAsync(
                    $"{LoggingPrefix}_delete",
                    async (childLogger) =>
                    {
                        childLogger.FluentAddBaseValue(JobQueueLoggerConst.JobId, queueMessage.Id);
                        var queue = await this.cloudQueueFactoryCallback();
                        var cloudQueueMessage = QueueMessageAdapter.AsCloudQueueMessage(queueMessage);
                        await queue.DeleteMessageAsync(cloudQueueMessage, cancellationToken);
                    },
                    swallowException: true);
            }

            /// <inheritdoc/>
            public Task UpdateMessageAsync(QueueMessage queueMessage, bool updateContent, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
            {
                return this.logger.OperationScopeAsync(
                    $"{LoggingPrefix}_update",
                    async (childLogger) =>
                    {
                        childLogger.FluentAddBaseValue(JobQueueLoggerConst.JobId, queueMessage.Id);
                        var queue = await this.cloudQueueFactoryCallback();
                        MessageUpdateFields messageUpdateFields =
                            MessageUpdateFields.Visibility | (updateContent ? MessageUpdateFields.Content : 0);

                        var cloudQueueMessage = QueueMessageAdapter.AsCloudQueueMessage(queueMessage);
                        await queue.UpdateMessageAsync(
                            cloudQueueMessage,
                            visibilityTimeout,
                            messageUpdateFields);
                    },
                    swallowException: true);
            }
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
