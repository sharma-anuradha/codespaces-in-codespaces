// <copyright file="BillingOverrideRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// the Billing Override repository which contains methods to get at specific billing override information.
    /// </summary>
    [DocumentDbCollectionId(EventCollectionId)]
    public class BillingOverrideRepository : DocumentDbCollection<BillingOverride>, IBillingOverrideRepository
    {
        /// <summary>
        /// The name of the collection.
        /// </summary>
        public const string EventCollectionId = "environment_billing_overrides";

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingOverrideRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">the health provider.</param>
        /// <param name="loggerFactory">the logger factory.</param>
        /// <param name="defaultLogValues">the log values.</param>
        public BillingOverrideRepository(
            IOptionsMonitor<DocumentDbCollectionOptions> options,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(
                options,
                clientProvider,
                healthProvider,
                loggerFactory,
                defaultLogValues)
        {
        }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        public static void ConfigureOptions(DocumentDbCollectionOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.PartitioningStrategy = PartitioningStrategy.None;
        }
    }
}
