// <copyright file="CrossRegionFrontendJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Queue;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common.Repositories.AzureQueue
{
    /// <summary>
    /// Resource repository fronting storage queue.
    /// </summary>
    public class CrossRegionFrontendJobQueueRepository : CrossRegionStorageQueueCollection, ICrossRegionContinuationJobQueueRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossRegionFrontendJobQueueRepository"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="controlPlaneInfo">Control plane info.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CrossRegionFrontendJobQueueRepository(
            ICrossRegionStorageQueueClientProvider clientProvider,
            IControlPlaneInfo controlPlaneInfo,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet defaultLogValues)
            : base(clientProvider, healthProvider, controlPlaneInfo, loggerFactory, resourceNameBuilder, defaultLogValues)
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
