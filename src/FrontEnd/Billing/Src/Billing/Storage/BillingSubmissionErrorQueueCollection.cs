// <copyright file="BillingSubmissionErrorQueueCollection.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Storage
{
    /// <summary>
    /// This represents the error queue collection
    /// </summary>
    public class BillingSubmissionErrorQueueCollection : StorageQueueCollection
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSubmissionErrorQueueCollection"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public BillingSubmissionErrorQueueCollection(
            IStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet defaultLogValues)
            : base(clientProvider, healthProvider, loggerFactory, resourceNameBuilder, defaultLogValues)
        {
        }

        protected override string LoggingDocumentName => "billing-submission-error-queue";

        protected override string QueueId => "error-reporting-queue";
    }
}
