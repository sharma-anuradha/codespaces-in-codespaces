// <copyright file="PlanRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    [DocumentDbCollectionId(PlanCollectionId)]
    public class PlanRepository : DocumentDbCollection<VsoPlan>, IPlanRepository
    {
        public const string PlanCollectionId = "environment_billing_plans";

        public PlanRepository(
            IOptions<DocumentDbCollectionOptions> collectionOptions,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(
                new DocumentDbCollectionOptionsSnapshot(collectionOptions, ConfigureOptions),
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
                // Partitioning on Subscription ID under the SkuPlan object
                "/plan/subscription",
            };
            options.CustomPartitionKeyFunc = (entity) =>
            {
                return new PartitionKey(((VsoPlan)entity).Plan?.Subscription);
            };
        }
    }
}
