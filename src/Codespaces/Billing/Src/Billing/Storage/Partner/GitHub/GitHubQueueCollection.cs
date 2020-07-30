// <copyright file="GitHubQueueCollection.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// A small wrapper around the StorageQueueCollection type to allow for billing specific queues.
    /// </summary>
    public class GitHubQueueCollection : StorageQueueCollection
    {
        private const string PartnerName = "github";

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubQueueCollection"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public GitHubQueueCollection(
            IStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet defaultLogValues)
            : base(clientProvider, healthProvider, loggerFactory, resourceNameBuilder, defaultLogValues)
        {
        }

        /// <inheritdoc/>
        protected sealed override string QueueId => ResourceNameBuilder.GetQueueName($"{PartnerName}-reporting-queue");

        /// <inheritdoc/>
        protected sealed override string LoggingDocumentName => QueueId;
    }
}
