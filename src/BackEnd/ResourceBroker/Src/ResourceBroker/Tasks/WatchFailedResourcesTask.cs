// <copyright file="WatchFailedResourcesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for failed resources.
    /// </summary>
    public class WatchFailedResourcesTask : BaseWatchPoolTask, IWatchFailedResourcesTask
    {
        private const int RequestedItems = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchFailedResourcesTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="continuationTaskActivator">Target continuation task activator.</param>
        /// <param name="resourceScalingStore">Target resource scaling store.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public WatchFailedResourcesTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IContinuationTaskActivator continuationTaskActivator,
            IResourcePoolDefinitionStore resourceScalingStore,
            IResourceRepository resourceRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder)
            : base(resourceBrokerSettings, resourceScalingStore, claimedDistributedLease, taskHelper, resourceNameBuilder)
        {
            ContinuationTaskActivator = continuationTaskActivator;
            ResourceRepository = resourceRepository;
        }

        /// <inheritdoc/>
        protected override string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchFailedResourcesTask)}Lease");

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.WatchFailedResourcesTask;

        private IContinuationTaskActivator ContinuationTaskActivator { get; }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        protected async override Task RunActionAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            var records = await ResourceRepository.GetFailedOperationAsync(
                resourcePool.Details.GetPoolDefinition(), RequestedItems, logger);

            logger.FluentAddValue("TaskRequestedItems", RequestedItems)
                .FluentAddValue("TaskFoundItems", records.Count());

            foreach (var record in records)
            {
                await logger.OperationScopeAsync(
                    $"{LogBaseName}_run_fail_cleanup",
                    () => RunFailCleanupAsync(record, logger.WithValues(new LogValueSet())),
                    swallowException: true);
            }
        }

        private async Task RunFailCleanupAsync(ResourceRecord record, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("TaskFailedItemRunId", Guid.NewGuid())
                .FluentAddBaseValue("ResourceId", record.Id);

            // Record the reason why this one is being deleted
            var didFailStatus = false;
            if (record.ProvisioningStatus == OperationState.Failed
                || record.ProvisioningStatus == OperationState.Cancelled
                || record.StartingStatus == OperationState.Failed
                || record.StartingStatus == OperationState.Cancelled
                || record.DeletingStatus == OperationState.Failed
                || record.DeletingStatus == OperationState.Cancelled)
            {
                didFailStatus = true;
            }

            logger.FluentAddValue("TaskFailedStatusItem", didFailStatus)
                .FluentAddValue("TaskFailedStalledItem", !didFailStatus);

            // Record which operation it failed on
            var didFailProvisioning = false;
            var didFailStarting = false;
            var didFailDeleting = false;
            if (record.ProvisioningStatus == OperationState.Failed
                || record.ProvisioningStatus == OperationState.Cancelled
                || ((record.ProvisioningStatus == OperationState.Initialized
                        || record.ProvisioningStatus == OperationState.InProgress)
                    && record.ProvisioningStatusChanged <= DateTime.UtcNow.AddHours(-1)))
            {
                didFailProvisioning = true;
            }
            else if (record.StartingStatus == OperationState.Failed
                || record.StartingStatus == OperationState.Cancelled
                || ((record.StartingStatus == OperationState.Initialized
                        || record.StartingStatus == OperationState.InProgress)
                    && record.StartingStatusChanged <= DateTime.UtcNow.AddHours(-1)))
            {
                didFailStarting = true;
            }
            else if (record.DeletingStatus == OperationState.Failed
                || record.DeletingStatus == OperationState.Cancelled
                || ((record.DeletingStatus == OperationState.Initialized
                        || record.DeletingStatus == OperationState.InProgress)
                    && record.DeletingStatusChanged <= DateTime.UtcNow.AddHours(-1)))
            {
                didFailDeleting = true;
            }

            logger.FluentAddValue("TaskDidFailProvisioning", didFailProvisioning)
                .FluentAddValue("TaskDidFailStarting", didFailStarting)
                .FluentAddValue("TaskDidFailDeleting", didFailDeleting);

            // Delete assuming we have something to do
            if (didFailProvisioning || didFailStarting || didFailDeleting)
            {
                logger.FluentAddValue("DeleteAttemptCount", record.DeleteAttemptCount);
                logger.LogWarning($"{LogBaseName}_stale_resource_found");

                // If we have already tried to delete 3 times, this time just delete the record
                if (record.DeleteAttemptCount >= 3)
                {
                    // Just delete the record, don't run through continuation
                    await ResourceRepository.DeleteAsync(record.Id, logger.WithValues(new LogValueSet()));
                    return;
                }

                // Kickoff delete continuation
                TaskHelper.RunBackground(
                    $"{LogBaseName}_delete",
                    (childLogger) => DeleteResourceItemAsync(Guid.Parse(record.Id), childLogger),
                    logger);
            }
            else
            {
                throw new Exception("Unexpected resource state while attempting to clean up resource.");
            }
        }

        private Task DeleteResourceItemAsync(Guid id, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_delete",
                async () =>
                {
                    logger.FluentAddBaseValue("ResourceId", id);
                    await ContinuationTaskActivator.DeleteResource(id, "WatchFailedResourcesTask", logger.WithValues(new LogValueSet()));
                },
                swallowException: true);
        }
    }
}
