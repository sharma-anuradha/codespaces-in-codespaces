// <copyright file="QueueResourceJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureQueue.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Sdk;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class QueueResourceJobQueueRepository : QueueCollectionBase, IResourceJobQueueRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueResourceJobQueueRepository"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public QueueResourceJobQueueRepository(
            IStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
        }

        /// <inheritdoc/>
        protected override string QueueId
        {
            get { return "resource-job-queue"; }
        }

        /// <inheritdoc/>
        public async Task AddAsync(string id, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueAsync();

            var message = new CloudQueueMessage(id);

            await queue.AddMessageAsync(message, null, initialVisibilityDelay, null, null);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<IResourceJobQueueMessage>> GetAsync(int popCount, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueAsync();

            var messages = await queue.GetMessagesAsync(popCount, TimeSpan.FromMinutes(5), null, null);

            return messages.Select(x => new QueueResourceJobQueueMessage(x));
        }

        /// <inheritdoc/>
        public async Task<IResourceJobQueueMessage> GetAsync(IDiagnosticsLogger logger)
        {
            return (await GetAsync(1, logger)).FirstOrDefault();
        }

        public async Task DeleteAsync(IResourceJobQueueMessage message, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueAsync();

            if (!(message is QueueResourceJobQueueMessage typedMessage))
            {
                throw new InvalidOperationException($"Supplied message isn't of the expected {nameof(QueueResourceJobQueueMessage)} type.");
            }

            await queue.DeleteMessageAsync(typedMessage.Message);
        }
    }
}
