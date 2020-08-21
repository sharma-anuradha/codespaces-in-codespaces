// <copyright file="EnvironmentExportAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Export Action.
    /// </summary>
    public class EnvironmentExportAction : EnvironmentBaseStartAction<EnvironmentExportActionInput, EnvironmentExportTransientState>, IEnvironmentExportAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentExportAction"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// <param name="skuCatalog">Target sku catalog.</param>
        /// <param name="skuUtils">Target skuUtils, to find sku's eligiblity.</param>
        /// <param name="planManager">Target plan manager.</param>
        /// <param name="subscriptionManager">Target subscription manager.</param>
        /// <param name="environmentSubscriptionManager">Target environnment subscription manager.</param>
        /// <param name="environmentManagerSettings">Target environment manager settings.</param>
        /// <param name="workspaceManager">Target workspace manager.</param>
        /// <param name="environmentMonitor">Target environment monitor.</param>
        /// <param name="environmentContinuation">Target environment continuation.</param>
        /// <param name="resourceAllocationManager">Target resource allocation manager.</param>
        /// <param name="resourceStartManager">Target resource start manager.</param>
        /// <param name="environmentSuspendAction">Target environment force suspend action.</param>
        /// <param name="resourceBrokerClient">Target resource broker client.</param>
        /// <param name="taskHelper">Target task helper.</param>
        public EnvironmentExportAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils,
            IPlanManager planManager,
            ISubscriptionManager subscriptionManager,
            IEnvironmentSubscriptionManager environmentSubscriptionManager,
            EnvironmentManagerSettings environmentManagerSettings,
            IWorkspaceManager workspaceManager,
            IEnvironmentMonitor environmentMonitor,
            IEnvironmentContinuationOperations environmentContinuation,
            IResourceAllocationManager resourceAllocationManager,
            IResourceStartManager resourceStartManager,
            IEnvironmentSuspendAction environmentSuspendAction,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerClient,
            ITaskHelper taskHelper)
           : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager, skuCatalog, skuUtils, planManager, subscriptionManager, environmentSubscriptionManager, environmentManagerSettings, workspaceManager, environmentMonitor, environmentContinuation, resourceAllocationManager, resourceStartManager, environmentSuspendAction, resourceBrokerClient, taskHelper)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_export_action";

        /// <inheritdoc/>
        public Task<CloudEnvironment> RunAsync(Guid environmentId, ExportCloudEnvironmentParameters exportEnvironmentParams, IDiagnosticsLogger logger)
        {
            // Base Validation
            Requires.NotEmpty(environmentId, nameof(environmentId));

            logger.AddEnvironmentId(environmentId.ToString());

            // Build input
            var input = new EnvironmentExportActionInput(environmentId)
            {
                ExportEnvironmentParams = exportEnvironmentParams,
            };

            return RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentExportActionInput input,
            EnvironmentExportTransientState transientState,
            IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger);

            await ConfigureRunCoreAsync(record, input, transientState, logger);

            if (record.Value.ExportedBlobUrl == null)
            {
                record.Value.ExportedBlobUrl = string.Empty;
            }

            return record.Value;
        }

        /// <inheritdoc/>
        protected override void ValidateInput(EnvironmentExportActionInput input)
        {
            ValidationUtil.IsRequired(input, nameof(input));
            ValidationUtil.IsRequired(input.ExportEnvironmentParams, nameof(input.ExportEnvironmentParams));
        }

        /// <inheritdoc/>
        protected override Task<EnvironmentTransition> AddWorkspaceConnection(EnvironmentTransition record, EnvironmentExportActionInput input, IDiagnosticsLogger logger)
        {
            return Task.FromResult(record);
        }

        /// <inheritdoc/>
        protected override async Task StartComputeAndMonitor(EnvironmentTransition record, ResourceAllocationRecord storageResource, ResourceAllocationRecord archiveStorageResource, EnvironmentExportActionInput input, IDiagnosticsLogger logger)
        {
            // Set up variables
            var computerResource = record.Value.Compute;
            var osDiskResource = record.Value.OSDisk;

            // Kick off start-compute before returning.
            await ResourceStartManager.StartComputeAsync(
                record.Value,
                computerResource.ResourceId,
                osDiskResource?.ResourceId,
                storageResource?.ResourceId,
                archiveStorageResource?.ResourceId,
                null,
                input.ExportEnvironmentParams,
                StartEnvironmentAction.StartExport,
                logger.NewChildLogger());

            // Export transition monitoring
            await EnvironmentMonitor.MonitorExportStateTransitionAsync(
                record.Value.Id,
                record.Value.Compute.ResourceId,
                logger.NewChildLogger());
        }

        /// <inheritdoc/>
        protected override async Task StartEnvironmentContinuation(EnvironmentTransition record, EnvironmentExportActionInput input, IDiagnosticsLogger logger)
        {
            await EnvironmentContinuation.ExportAsync(
                   Guid.Parse(record.Value.Id),
                   record.Value.LastStateUpdated,
                   input.ExportEnvironmentParams,
                   "exportenvironment",
                   logger.NewChildLogger());
        }

        /// <inheritdoc/>
        protected override void ValidateEnvironmentState(CloudEnvironment environment)
        {
            // Cannot export if the state is not Exporting or Queued
            // Todo: elpadann - standardize using the action state allow list which aaronpaskin is working on.
            ValidationUtil.IsTrue(
                environment.State == CloudEnvironmentState.Exporting || environment.State == CloudEnvironmentState.Queued,
                "Environment is not in an exportable state.");
        }
    }
}
