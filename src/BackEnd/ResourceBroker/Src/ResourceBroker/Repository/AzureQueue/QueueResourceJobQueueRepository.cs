// <copyright file="QueueResourceJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Sdk;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureQueue
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
        public async Task AddAsync(string id, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueAsync();

            var message = new CloudQueueMessage(id);

            await queue.AddMessageAsync(message);
        }
    }
}
