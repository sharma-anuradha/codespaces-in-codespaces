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
        /// <param name="resourceContinuationOperations">Target continuation task activator.</param>
        /// <param name="resourceScalingStore">Target resource scaling store.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public WatchFailedResourcesTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourceContinuationOperations resourceContinuationOperations,
            IResourcePoolDefinitionStore resourceScalingStore,
            IResourceRepository resourceRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder)
            : base(resourceBrokerSettings, resourceScalingStore, claimedDistributedLease, taskHelper, resourceNameBuilder)
        {
            ResourceContinuationOperations = resourceContinuationOperations;
            ResourceRepository = resourceRepository;
        }

        /// <inheritdoc/>
        protected override string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchFailedResourcesTask)}Lease");

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.WatchFailedResourcesTask;

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        protected async override Task RunActionAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            var records = await ResourceRepository.GetFailedOperationAsync(
                resourcePool.Details.GetPoolDefinition(), RequestedItems, logger.NewChildLogger());

            logger.FluentAddValue("TaskRequestedItems", RequestedItems)
                .FluentAddValue("TaskFoundItems", records.Count());

            foreach (var record in records)
            {
                await RunFailCleanupAsync(record, logger);
            }
        }

        private async Task RunFailCleanupAsync(ResourceRecord record, IDiagnosticsLogger loogger)
        {
            await loogger.OperationScopeAsync(
                $"{LogBaseName}_run_fail_cleanup",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("TaskFailedItemRunId", Guid.NewGuid())
                        .FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, record.Id);

                    // Record the reason why this one is being deleted
                    var didFailStatus = false;
                    if (record.ProvisioningStatus == null
                        || record.ProvisioningStatus == OperationState.Failed
                        || record.ProvisioningStatus == OperationState.Cancelled
                        || record.StartingStatus == OperationState.Failed
                        || record.StartingStatus == OperationState.Cancelled
                        || record.DeletingStatus == OperationState.Failed
                        || record.DeletingStatus == OperationState.Cancelled)
                    {
                        didFailStatus = true;
                    }

                    childLogger.FluentAddValue("TaskFailedStatusItem", didFailStatus)
                        .FluentAddValue("TaskFailedStalledItem", !didFailStatus);

                    // Record which operation it failed on
                    var reason = "";
                    var didFailProvisioning = false;
                    var didFailStarting = false;
                    var didFailDeleting = false;
                    var operationFailedTimeLimit = DateTime.UtcNow.AddHours(-1);
                    if (record.ProvisioningStatus == OperationState.Failed
                        || record.ProvisioningStatus == OperationState.Cancelled
                        || ((record.ProvisioningStatus == OperationState.Initialized
                                || record.ProvisioningStatus == OperationState.InProgress)
                            && record.ProvisioningStatusChanged <= operationFailedTimeLimit)
                        || (record.ProvisioningStatus == null
                            && record.Created <= operationFailedTimeLimit))
                    {
                        didFailProvisioning = true;
                        reason = (record.ProvisioningStatus == null) ? "FailProvisioningNullStatus" : "FailProvisioning";
                    }
                    else if (record.StartingStatus == OperationState.Failed
                        || record.StartingStatus == OperationState.Cancelled
                        || ((record.StartingStatus == OperationState.Initialized
                                || record.StartingStatus == OperationState.InProgress)
                            && record.StartingStatusChanged <= operationFailedTimeLimit))
                    {
                        didFailStarting = true;
                        reason = "FailStarting";
                    }
                    else if (record.DeletingStatus == OperationState.Failed
                        || record.DeletingStatus == OperationState.Cancelled
                        || ((record.DeletingStatus == OperationState.Initialized
                                || record.DeletingStatus == OperationState.InProgress)
                            && record.DeletingStatusChanged <= operationFailedTimeLimit))
                    {
                        didFailDeleting = true;
                        reason = "FailDeleting";
                    }

                    childLogger.FluentAddValue("TaskDidFailProvisioning", didFailProvisioning)
                        .FluentAddValue("TaskDidFailStarting", didFailStarting)
                        .FluentAddValue("TaskDidFailDeleting", didFailDeleting)
                        .FluentAddValue("TaskDidFailReason", reason);

                    // Delete assuming we have something to do
                    if (didFailProvisioning || didFailStarting || didFailDeleting)
                    {
                        childLogger.FluentAddValue("DeleteAttemptCount", record.DeleteAttemptCount);
                        childLogger.LogWarning($"{LogBaseName}_stale_resource_found");

                        // If we have already tried to delete 3 times, this time just delete the record
                        if (record.DeleteAttemptCount >= 3)
                        {
                            // Just delete the record, don't run through continuation
                            await childLogger.OperationScopeAsync(
                                $"{LogBaseName}_delete_record",
                                (deleteLogger) => DeleteResourceAsync(record.Id, reason, deleteLogger));

                            return;
                        }

                        // Kickoff delete continuation
                        TaskHelper.RunBackground(
                            $"{LogBaseName}_delete",
                            (taskLogger) => DeleteResourceItemAsync(Guid.Parse(record.Id), taskLogger),
                            childLogger);
                    }
                    else
                    {
                        throw new Exception("Unexpected resource state while attempting to clean up resource.");
                    }
                },
                swallowException: true);
        }

        private async Task DeleteResourceAsync(string id, string reason, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, id)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason);

            // Since we don't have the azyre resource, we are just goignt to delete this record
            await ResourceRepository.DeleteAsync(id, logger.NewChildLogger());
        }

        private async Task DeleteResourceItemAsync(Guid id, IDiagnosticsLogger logger)
        {
            var reason = "WatchFailedResourcesTask";

            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, id)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason);

            await ResourceContinuationOperations.DeleteResource(id, reason, logger.NewChildLogger());
        }
    }
}
