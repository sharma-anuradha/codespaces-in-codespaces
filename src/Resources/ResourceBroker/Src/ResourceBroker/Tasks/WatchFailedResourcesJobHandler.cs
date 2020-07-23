// <copyright file="WatchFailedResourcesJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for failed resources.
    /// </summary>
    public class WatchFailedResourcesJobHandler : WatchPoolJobHandlerBase<WatchFailedResourcesJobHandler>
    {
        private const int RequestedItems = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchFailedResourcesJobHandler"/> class.
        /// </summary>
        /// <param name="resourcePoolDefinitionStore">Resource pool definition store.</param>
        /// <param name="resourceContinuationOperations">Target continuation task activator.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        public WatchFailedResourcesJobHandler(
            IResourcePoolDefinitionStore resourcePoolDefinitionStore,
            IResourceContinuationOperations resourceContinuationOperations,
            IResourceRepository resourceRepository,
            ITaskHelper taskHelper)
            : base(resourcePoolDefinitionStore, taskHelper)
        {
            ResourceContinuationOperations = resourceContinuationOperations;
            ResourceRepository = resourceRepository;
        }

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        private IResourceRepository ResourceRepository { get; }

        private string LogBaseName => ResourceLoggingConstants.WatchPoolSizeJobHandler;

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(ResourcePool resourcePool, IDiagnosticsLogger logger, CancellationToken cancellationToken)
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
                    var reason = string.Empty;
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
                            && record.Created <= operationFailedTimeLimit)
                        || (record.ProvisioningStatus == OperationState.Succeeded
                            && record.IsReady == false
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

                    // Delete assuming we have something to do. Double check that only VMs are being deleted if they failed to start.
                    if (didFailProvisioning || didFailDeleting || (didFailStarting && record.Type == ResourceType.ComputeVM))
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
                        throw new InvalidOperationException("Unexpected resource state while attempting to clean up resource.");
                    }
                },
                swallowException: true);
        }

        private async Task DeleteResourceAsync(string id, string reason, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, id)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason);

            // Since we don't have the azure resource, we are just going to delete this record
            await ResourceRepository.DeleteAsync(id, logger.NewChildLogger());
        }

        private async Task DeleteResourceItemAsync(Guid id, IDiagnosticsLogger logger)
        {
            var reason = "WatchFailedResourcesTask";

            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, id)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason);

            await ResourceContinuationOperations.DeleteAsync(null, id, reason, logger.NewChildLogger());
        }
    }
}
