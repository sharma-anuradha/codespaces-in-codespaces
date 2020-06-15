// <copyright file="FrontendJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common.Repositories.AzureQueue
{
    /// <summary>
    /// Resource repository fronting storage queue.
    /// </summary>
    public class FrontendJobQueueRepository : StorageQueueCollection, IContinuationJobQueueRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrontendJobQueueRepository"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public FrontendJobQueueRepository(
            IStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet defaultLogValues)
            : base(clientProvider, healthProvider, loggerFactory, resourceNameBuilder, defaultLogValues)
        {
        }

        /// <inheritdoc/>
        /// <summary>
        /// Queue for all frontend jobs.
        /// Queue id is kept as "environment-job-queue" for backward compatibility.
        /// </summary>
        protected override string QueueId => ResourceNameBuilder.GetQueueName("environment-job-queue");

        /// <inheritdoc/>
        /// <summary>
        /// LoggingDocumentName is left as "environment" for backward compatibility.
        /// </summary>
        protected override string LoggingDocumentName => "environment";
    }
}
