// <copyright file="BillingEventRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// A collection for all billing events.
    /// </summary>
    [DocumentDbCollectionId(EventCollectionId)]
    public class BillingEventRepository : DocumentDbCollection<BillingEvent>, IBillingEventRepository
    {
        /// <summary>
        /// The name of the collection.
        /// </summary>
        public const string EventCollectionId = "environment_billing_events";

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingEventRepository"/> class.
        /// </summary>
        /// <param name="options">DB options.</param>
        /// <param name="clientProvider">The docDB Client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">Used to generate a logger for queries.</param>
        /// <param name="defaultLogValues">A set of log values.</param>
        public BillingEventRepository(
            IOptions<DocumentDbCollectionOptions> options,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(
                new DocumentDbCollectionOptionsSnapshot(options, ConfigureOptions),
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
            options.PartitioningStrategy = PartitioningStrategy.Custom;
            options.CustomPartitionKeyPaths = new[]
            {
                // Billing events are partitioned by subscription ID. Most queries
                // will filter on a specific plan, which includes a subscription ID.
                "/plan/subscription",
            };
            options.CustomPartitionKeyFunc = (entity) =>
            {
                return new PartitionKey(((BillingEvent)entity).Plan?.Subscription);
            };
        }
    }
}
