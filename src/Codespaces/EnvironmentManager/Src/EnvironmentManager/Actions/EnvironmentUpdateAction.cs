// <copyright file="EnvironmentUpdateAction.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment update action.
    /// </summary>
    public class EnvironmentUpdateAction : EnvironmentBaseStartAction<EnvironmentUpdateActionInput, EnvironmentUpdateTransientState>, IEnvironmentUpdateAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentUpdateAction"/> class.
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
        /// <param name="currentIdentityProvider">Current identity provider.</param>
        /// <param name="superuserIdentity">Super user identity.</param>
        public EnvironmentUpdateAction(
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
            ITaskHelper taskHelper,
            ICurrentIdentityProvider currentIdentityProvider,
            VsoSuperuserClaimsIdentity superuserIdentity)
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
            CurrentIdentityProvider = Requires.NotNull(currentIdentityProvider, nameof(currentIdentityProvider));
            SuperuserIdentity = Requires.NotNull(superuserIdentity, nameof(superuserIdentity));
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_update_action";

        private ICurrentIdentityProvider CurrentIdentityProvider { get; }

        private VsoSuperuserClaimsIdentity SuperuserIdentity { get; }

        /// <inheritdoc/>
        public Task<CloudEnvironment> RunAsync(Guid environmentId, CloudEnvironmentParameters cloudEnvironmentParams, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            logger.AddEnvironmentId(environmentId.ToString());

            var input = new EnvironmentUpdateActionInput(environmentId)
            {
                CloudEnvironmentParameters = cloudEnvironmentParams,
            };

            return RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentUpdateActionInput input,
            EnvironmentUpdateTransientState transientState,
            IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger);

            await ConfigureRunCoreAsync(record, input, transientState, logger);

            return record.Value;
        }

        /// <inheritdoc/>
        protected override void ValidateInput(EnvironmentUpdateActionInput input)
        {
            ValidationUtil.IsRequired(input, nameof(input));
            ValidationUtil.IsRequired(input.CloudEnvironmentParameters, nameof(input.CloudEnvironmentParameters));
        }

        /// <inheritdoc/>
        protected override Task<EnvironmentTransition> AddWorkspaceConnection(EnvironmentTransition record, EnvironmentUpdateActionInput input, IDiagnosticsLogger logger)
        {
            return Task.FromResult(record);
        }

        /// <inheritdoc/>
        protected override Task<EnvironmentTransition> FetchAsync(EnvironmentUpdateActionInput input, IDiagnosticsLogger logger)
        {
            if (CurrentIdentityProvider.Identity?.IsSuperuser() == true)
            {
                return base.FetchAsync(input, logger);
            }

            using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
            {
                return base.FetchAsync(input, logger);
            }
        }

        protected override Task<EnvironmentTransition> FetchOrGetDefaultAsync(EnvironmentUpdateActionInput input, IDiagnosticsLogger logger)
        {
            if (CurrentIdentityProvider.Identity?.IsSuperuser() == true)
            {
                return base.FetchOrGetDefaultAsync(input, logger);
            }

            using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
            {
                return base.FetchOrGetDefaultAsync(input, logger);
            }
        }

        /// <inheritdoc/>
        protected override async Task StartComputeAndMonitor(
            EnvironmentTransition record,
            ResourceAllocationRecord storageResource,
            ResourceAllocationRecord archiveStorageResource,
            EnvironmentUpdateActionInput input,
            IDiagnosticsLogger logger)
        {
            // Kick-off start-compute before returning
            using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
            {
                await ResourceStartManager.StartComputeAsync(
                    record.Value,
                    record.Value.Compute.ResourceId,
                    record.Value.OSDisk?.ResourceId,
                    storageResource?.ResourceId,
                    archiveStorageResource?.ResourceId,
                    null,
                    input.CloudEnvironmentParameters,
                    StartEnvironmentAction.StartUpdate,
                    logger.NewChildLogger());
            }

            // Update transtion monitoring
            await EnvironmentMonitor.MonitorUpdateStateTransitionAsync(
                record.Value.Id,
                record.Value.Compute.ResourceId,
                logger.NewChildLogger());
        }

        /// <inheritdoc/>
        protected override async Task StartEnvironmentContinuation(
            EnvironmentTransition record,
            EnvironmentUpdateActionInput input,
            IDiagnosticsLogger logger)
        {
            await EnvironmentContinuation.UpdateSystemAsync(
                Guid.Parse(record.Value.Id),
                record.Value.LastStateUpdated,
                input.CloudEnvironmentParameters,
                "updateenvironment",
                logger.NewChildLogger());
        }

        /// <inheritdoc/>
        protected override void ValidateEnvironmentState(CloudEnvironment environment)
        {
            ValidationUtil.IsTrue(
                environment.State == CloudEnvironmentState.Updating || environment.State == CloudEnvironmentState.Queued,
                "Environment is not in an updatable state.");
        }
    }
}
