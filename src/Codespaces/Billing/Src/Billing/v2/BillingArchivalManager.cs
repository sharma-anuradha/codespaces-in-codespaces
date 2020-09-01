// <copyright file="BillingArchivalManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Responsible for archiving billing records no longer actively needed.
    /// </summary>
    public class BillingArchivalManager : IBillingArchivalManager
    {
        private const string BaseLogName = "billing_archival_manager";
        private readonly BillingSettings billingSettings;
        private readonly IBillSummaryRepository billSummaryRepository;
        private readonly IBillSummaryArchiveRepository billSummaryArchiveRepository;
        private readonly IEnvironmentStateChangeRepository stateChangeRepository;
        private readonly IEnvironmentStateChangeArchiveRepository stateChangeArchiveRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingArchivalManager"/> class.
        /// </summary>
        /// <param name="billingSettings">Billing Settings.</param>
        /// <param name="billSummaryRepository">bill Summary Repository.</param>
        /// <param name="billSummaryArchiveRepository">bill Summary Archive Repository.</param>
        /// <param name="stateChangeRepository">state Change Repository.</param>
        /// <param name="stateChangeArchiveRepository">environment State Change Archive Repository.</param>
        public BillingArchivalManager(
            BillingSettings billingSettings,
            IBillSummaryRepository billSummaryRepository,
            IBillSummaryArchiveRepository billSummaryArchiveRepository,
            IEnvironmentStateChangeRepository stateChangeRepository,
            IEnvironmentStateChangeArchiveRepository stateChangeArchiveRepository)
        {
            this.billingSettings = billingSettings;
            this.billSummaryRepository = billSummaryRepository;
            this.billSummaryArchiveRepository = billSummaryArchiveRepository;
            this.stateChangeRepository = stateChangeRepository;
            this.stateChangeArchiveRepository = stateChangeArchiveRepository;
        }

        /// <inheritdoc/>
        public Task MigrateEnvironmentStateChange(EnvironmentStateChange stateChange, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{BaseLogName}_migrate_environment_state_change",
                async (childLogger) =>
                {
                    bool enableArchiving = await billingSettings.V2EnableArchivingAsync(logger.NewChildLogger());
                    childLogger.FluentAddValue("EnableArchiving", enableArchiving);

                    if (enableArchiving)
                    {
                        var sourcePartitionKey = EnvironmentStateChange.CreateActivePartitionKey(stateChange.PlanId);
                        var destinationPartitionKey = EnvironmentStateChange.CreateArchivedPartitionKey(stateChange.PlanId, stateChange.Time);
                        await MigrateAsync(
                            stateChangeRepository,
                            stateChangeArchiveRepository,
                            sourcePartitionKey,
                            destinationPartitionKey,
                            stateChange,
                            logger);
                    }
                });
        }

        /// <inheritdoc/>
        public Task MigrateBillSummary(BillSummary billSummary, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{BaseLogName}_migrate_bill_summary",
                async (childLogger) =>
                {
                    bool enableArchiving = await billingSettings.V2EnableArchivingAsync(logger.NewChildLogger());
                    childLogger.FluentAddValue("EnableArchiving", enableArchiving);

                    if (enableArchiving)
                    {
                        var sourcePartitionKey = BillSummary.CreateActivePartitionKey(billSummary.PlanId);
                        var destinationPartitionKey = BillSummary.CreateArchivedPartitionKey(billSummary.PlanId, billSummary.BillGenerationTime);
                        await MigrateAsync(
                            billSummaryRepository,
                            billSummaryArchiveRepository,
                            sourcePartitionKey,
                            destinationPartitionKey,
                            billSummary,
                            logger);
                    }
                });
        }

        private static Task MigrateAsync<T>(
            IDocumentDbCollection<T> source,
            IDocumentDbCollection<T> destination,
            string sourcePartitionKey,
            string destinationPartitionKey,
            T entity,
            IDiagnosticsLogger logger)
            where T : CosmosDbEntity
        {
            return logger.OperationScopeAsync(
                $"{BaseLogName}_migrate",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue("Id", entity.Id);
                    childLogger.FluentAddValue("SourcePartitionKey", sourcePartitionKey);
                    childLogger.FluentAddValue("DestinationPartitionKey", destinationPartitionKey);

                    var sourceKey = new DocumentDbKey(entity.Id, new PartitionKey(sourcePartitionKey));
                    var destinationKey = new DocumentDbKey(entity.Id, new PartitionKey(destinationPartitionKey));

                    var isArchived = await childLogger.RetryOperationScopeAsync(
                        $"{BaseLogName}_migrate_check_archived_status",
                        async (innerLogger) =>
                        {
                            var item = await destination.GetAsync(destinationKey, innerLogger);
                            return item != null;
                        });

                    childLogger.FluentAddValue("ArchivedEntityAlreadyExists", isArchived);

                    if (!isArchived)
                    {
                        // create the record first so the method is re-runnable if create or delete fails
                        var archivedEntity = await destination.CreateAsync(entity, childLogger);
                    }

                    // delete the source
                    await childLogger.RetryOperationScopeAsync(
                        $"{BaseLogName}_migrate_delete_source_item",
                        async (innerLogger) =>
                        {
                            await source.DeleteAsync(sourceKey, innerLogger);
                        });
                });
        }
    }
}
