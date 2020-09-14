// <copyright file="EnvironmentResumeAction.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Resume Action.
    /// </summary>
    public class EnvironmentResumeAction : EnvironmentBaseStartAction<EnvironmentResumeActionInput, EnvironmentResumeTransientState>, IEnvironmentResumeAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentResumeAction"/> class.
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
        /// <param name="workspaceManager">Target workspace manager.</param>
        /// <param name="environmentMonitor">Target environment monitor.</param>
        /// <param name="environmentContinuation">Target environment continuation.</param>
        /// <param name="resourceAllocationManager">Target resource allocation manager.</param>
        /// <param name="resourceStartManager">Target resource start manager.</param>
        /// <param name="environmentSuspendAction">Target environment force suspend action.</param>
        /// <param name="resourceBrokerClient">Target resource broker client.</param>
        /// <param name="taskHelper">Target task helper.</param>
        public EnvironmentResumeAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils,
            IPlanManager planManager,
            IWorkspaceManager workspaceManager,
            IEnvironmentMonitor environmentMonitor,
            IEnvironmentContinuationOperations environmentContinuation,
            IResourceAllocationManager resourceAllocationManager,
            IResourceStartManager resourceStartManager,
            IEnvironmentSuspendAction environmentSuspendAction,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerClient,
            ITaskHelper taskHelper)
           : base(
                 environmentStateManager,
                 repository,
                 currentLocationProvider,
                 currentUserProvider,
                 controlPlaneInfo,
                 environmentAccessManager,
                 skuCatalog,
                 skuUtils,
                 planManager,
                 workspaceManager,
                 environmentMonitor,
                 environmentContinuation,
                 resourceAllocationManager,
                 resourceStartManager,
                 environmentSuspendAction,
                 resourceBrokerClient,
                 taskHelper)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_resume_action";

        /// <inheritdoc/>
        public Task<CloudEnvironment> RunAsync(Guid environmentId, StartCloudEnvironmentParameters startEnvironmentParams, IDiagnosticsLogger logger)
        {
            // Base Validation
            Requires.NotEmpty(environmentId, nameof(environmentId));
            logger.AddEnvironmentId(environmentId.ToString());

            // Build input
            var input = new EnvironmentResumeActionInput(environmentId)
            {
                StartEnvironmentParams = startEnvironmentParams,
            };

            return RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentResumeActionInput input,
            EnvironmentResumeTransientState transientState,
            IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger);

            // No action required if the environment is already running
            if (record.Value.State == CloudEnvironmentState.Available)
            {
                return record.Value;
            }

            await ConfigureRunCoreAsync(record, input, transientState, logger);

            return record.Value;
        }

        /// <inheritdoc/>
        protected override async Task<EnvironmentTransition> AddWorkspaceConnection(EnvironmentTransition record, EnvironmentResumeActionInput input, IDiagnosticsLogger logger)
        {
            var connectionInfo = await WorkspaceManager.CreateWorkspaceAsync(
                           EnvironmentType.CloudEnvironment,
                           record.Value.Id,
                           record.Value.Compute.ResourceId,
                           input.StartEnvironmentParams.ConnectionServiceUri,
                           record.Value.Connection?.ConnectionSessionPath,
                           input.StartEnvironmentParams.UserProfile.Email,
                           input.StartEnvironmentParams.UserProfile.Id,
                           record.Value.Partner == Partner.GitHub,
                           null,
                           logger.NewChildLogger());
            record.PushTransition((environment) =>
            {
                environment.Connection = connectionInfo;
            });
            return record;
        }

        /// <inheritdoc/>
        protected override async Task StartComputeAndMonitor(EnvironmentTransition record, ResourceAllocationRecord storageResource, ResourceAllocationRecord archiveStorageResource, EnvironmentResumeActionInput input, IDiagnosticsLogger logger)
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
                input.StartEnvironmentParams,
                StartEnvironmentAction.StartCompute,
                logger.NewChildLogger());

            // Resume transition monitoring
            await EnvironmentMonitor.MonitorResumeStateTransitionAsync(
                record.Value.Id,
                record.Value.Compute.ResourceId,
                logger.NewChildLogger());
        }

        /// <inheritdoc/>
        protected override async Task StartEnvironmentContinuation(EnvironmentTransition record, EnvironmentResumeActionInput input, IDiagnosticsLogger logger)
        {
            await EnvironmentContinuation.ResumeAsync(
                   Guid.Parse(record.Value.Id),
                   record.Value.LastStateUpdated,
                   input.StartEnvironmentParams,
                   "resumeenvironment",
                   logger.NewChildLogger());
        }

        /// <inheritdoc/>
        protected override void ValidateInput(EnvironmentResumeActionInput input)
        {
            ValidationUtil.IsRequired(input, nameof(input));
            ValidationUtil.IsRequired(input.StartEnvironmentParams, nameof(input.StartEnvironmentParams));
        }

        /// <inheritdoc/>
        protected override void ValidateEnvironmentState(CloudEnvironment environment)
        {
            // Cannot resume if the state is not Starting or Queued
            // Todo: elpadann - standardize using the action state allow list which aaronpaskin is working on.
            ValidationUtil.IsTrue(
                environment.State == CloudEnvironmentState.Starting || environment.State == CloudEnvironmentState.Queued,
                "Environment is not in a resumable state.");
        }
    }
}
