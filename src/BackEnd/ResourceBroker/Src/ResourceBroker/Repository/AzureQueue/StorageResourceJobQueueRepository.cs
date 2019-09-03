// <copyright file="StorageResourceJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Sdk;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource repository fronting storage queue.
    /// </summary>
    public class StorageResourceJobQueueRepository : StorageQueueCollection, IResourceJobQueueRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageResourceJobQueueRepository"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public StorageResourceJobQueueRepository(
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
            get => "resource-job-queue";
        }

        /// <inheritdoc/>
        protected override string LoggingDocumentName
        {
            get => "resource";
        }
    }
}
