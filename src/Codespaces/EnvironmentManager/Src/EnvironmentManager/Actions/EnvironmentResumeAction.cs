// <copyright file="EnvironmentResumeAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Resume Action.
    /// </summary>
    public class EnvironmentResumeAction : EnvironmentItemAction<EnvironmentResumeActionInput, EnvironmentResumeTransientState>, IEnvironmentResumeAction
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
            ISubscriptionManager subscriptionManager,
            IEnvironmentSubscriptionManager environmentSubscriptionManager,
            EnvironmentManagerSettings environmentManagerSettings,
            IWorkspaceManager workspaceManager,
            IEnvironmentMonitor environmentMonitor,
            IEnvironmentContinuationOperations environmentContinuation,
            IResourceAllocationManager resourceAllocationManager,
            IResourceStartManager resourceStartManager,
            IEnvironmentSuspendAction environmentSuspendAction,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerClient)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager, skuCatalog, skuUtils)
        {
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            SubscriptionManager = Requires.NotNull(subscriptionManager, nameof(subscriptionManager));
            EnvironmentSubscriptionManager = Requires.NotNull(environmentSubscriptionManager, nameof(environmentSubscriptionManager));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
            WorkspaceManager = Requires.NotNull(workspaceManager, nameof(workspaceManager));
            EnvironmentMonitor = Requires.NotNull(environmentMonitor, nameof(environmentMonitor));
            EnvironmentContinuation = Requires.NotNull(environmentContinuation, nameof(environmentContinuation));
            ResourceAllocationManager = Requires.NotNull(resourceAllocationManager, nameof(resourceAllocationManager));
            ResourceStartManager = Requires.NotNull(resourceStartManager, nameof(resourceStartManager));
            EnvironmentSuspendAction = Requires.NotNull(environmentSuspendAction, nameof(environmentSuspendAction));
            ResourceBrokerClient = Requires.NotNull(resourceBrokerClient, nameof(resourceBrokerClient));
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_resume_action";

        private IPlanManager PlanManager { get; }

        private ISubscriptionManager SubscriptionManager { get; }

        private IEnvironmentSubscriptionManager EnvironmentSubscriptionManager { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private IWorkspaceManager WorkspaceManager { get; }

        private IEnvironmentMonitor EnvironmentMonitor { get; }

        private IEnvironmentContinuationOperations EnvironmentContinuation { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IResourceStartManager ResourceStartManager { get; }

        private IEnvironmentSuspendAction EnvironmentSuspendAction { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> Run(Guid environmentId, StartCloudEnvironmentParameters startEnvironmentParams, IDiagnosticsLogger logger)
        {
            // Base Validation
            Requires.NotEmpty(environmentId, nameof(environmentId));
            logger.AddEnvironmentId(environmentId.ToString());

            // Build input
            var input = new EnvironmentResumeActionInput(environmentId)
            {
                StartEnvironmentParams = startEnvironmentParams,
            };

            return await Run(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentResumeActionInput input,
            EnvironmentResumeTransientState transientState,
            IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger);

            // No action required if the environment is alredy running, or being resumed
            if (record.Value.State == CloudEnvironmentState.Starting ||
                        record.Value.State == CloudEnvironmentState.Available)
            {
                return record.Value;
            }

            var plan = await FetchPlanAsync(record.Value, logger.NewChildLogger());

            // Validate
            ValidateInput(input);
            await ValidateEnvironmentAsync(record.Value, plan, logger);
            await ValidatePlanAndSubscriptionAsync(record.Value, plan, logger);

            // Authorize
            EnvironmentAccessManager.AuthorizeEnvironmentAccess(record.Value, nonOwnerScopes: null, logger);

            // Add VNet information to environment
            var subnetId = plan.Properties?.VnetProperties?.SubnetId;
            record.Value.SubnetResourceId = subnetId;

            var connectionWorkspaceRootId = record.Value.Connection?.WorkspaceId;
            if (!string.IsNullOrWhiteSpace(connectionWorkspaceRootId))
            {
                // Delete the previous liveshare session from database.
                // Do not block start process on delete of old workspace from liveshare db.
                _ = Task.Run(() => WorkspaceManager.DeleteWorkspaceAsync(connectionWorkspaceRootId, logger.NewChildLogger()));
                record.Value.Connection.ConnectionComputeId = null;
                record.Value.Connection.ConnectionComputeTargetId = null;
                record.Value.Connection.ConnectionServiceUri = null;
                record.Value.Connection.ConnectionSessionId = null;
                record.Value.Connection.WorkspaceId = null;
            }

            SkuCatalog.CloudEnvironmentSkus.TryGetValue(record.Value.SkuName, out var sku);
            if (sku.ComputeOS == ComputeOS.Windows || !string.IsNullOrEmpty(record.Value.SubnetResourceId))
            {
                // Windows can only be queued resume because the VM has to be constructed from the given OS disk.
                await QueueResumeEnvironmentAsync(input, record, transientState, logger.NewChildLogger());
            }
            else
            {
                await ResumeEnvironmentAsync(input, record, transientState, logger.NewChildLogger());
            }

            return record.Value;
        }

        /// <inheritdoc/>
        protected override async Task<bool> HandleExceptionAsync(
            EnvironmentResumeActionInput input,
            Exception ex,
            EnvironmentResumeTransientState transientState,
            IDiagnosticsLogger logger)
        {
            var isFullyHandled = false;

            // Suspend the environment if the state is already changed to starting/queued
            if (transientState.CloudEnvironmentState == CloudEnvironmentState.Starting || transientState.CloudEnvironmentState == CloudEnvironmentState.Queued)
            {
                await EnvironmentSuspendAction.Run(input.Id, transientState.AllocatedComputeId, logger.NewChildLogger());
            }
            else if (transientState.AllocatedComputeId != default)
            {
                // Delete the allocated resources.
                await ResourceBrokerClient.DeleteAsync(input.Id, transientState.AllocatedComputeId, logger.NewChildLogger());
            }

            if (ex is EnvironmentMonitorInitializationException)
            {
                // Todo: elpadann - this is bad logging pattern, revise it.
                logger.NewChildLogger().LogException($"{LogBaseName}_create_monitor_error", ex);
                throw new UnavailableException((int)MessageCodes.UnableToAllocateResourcesWhileStarting, ex.Message, ex);
            }

            // If the code made this far, the exception is not fully handled.
            return isFullyHandled;
        }

        private async Task ResumeEnvironmentAsync(
            EnvironmentResumeActionInput input,
            EnvironmentTransition record,
            EnvironmentResumeTransientState transientState,
            IDiagnosticsLogger logger)
        {
            // Allocate Compute
            record.Value.Compute = await AllocateComputeAsync(record.Value, logger.NewChildLogger());

            // Set compute id in to transientState object,
            // so that it can be used for deallocating in case of an exception.
            transientState.AllocatedComputeId = record.Value.Compute.ResourceId;

            // Start Environment Monitoring
            await EnvironmentMonitor.MonitorHeartbeatAsync(record.Value.Id, record.Value.Compute.ResourceId, logger.NewChildLogger());

            // Create the Live Share workspace
            record.Value.Connection = await WorkspaceManager.CreateWorkspaceAsync(
                EnvironmentType.CloudEnvironment,
                record.Value.Id,
                record.Value.Compute.ResourceId,
                input.StartEnvironmentParams.ConnectionServiceUri,
                record.Value.Connection?.ConnectionSessionPath,
                input.StartEnvironmentParams.UserProfile.Email,
                null,
                logger.NewChildLogger());

            // Setup variables for easier use
            var computerResource = record.Value.Compute;
            var storageResource = record.Value.Storage;
            var osDiskResource = record.Value.OSDisk;
            var archiveStorageResource = storageResource.Type == ResourceType.StorageArchive
                ? storageResource : null;
            var isArchivedEnvironment = archiveStorageResource != null;

            logger.AddCloudEnvironmentIsArchived(isArchivedEnvironment);

            // At this point, if archive record is going to be switched in it will have been
            var startingStateReson = isArchivedEnvironment ? MessageCodes.RestoringFromArchive.ToString() : null;
            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record.Value,
                CloudEnvironmentState.Starting,
                CloudEnvironmentStateUpdateTriggers.StartEnvironment,
                startingStateReson,
                null,
                logger.NewChildLogger());

            // Set environment state in to transientState object,
            // so that it can be used to suspend/force suspend in case of an exception.
            transientState.CloudEnvironmentState = record.Value.State;

            record.Value.Transitions.ShuttingDown.ResetStatus(true);

            // Persist updates made to date
            var updatedEnvironment = await Repository.UpdateAsync(record.Value, logger.NewChildLogger());
            record.ReplaceAndResetTransition(updatedEnvironment);

            // Provision new storage if environment has been archvied but don't switch until complete
            if (archiveStorageResource != null)
            {
                storageResource = await AllocateStorageAsync(record.Value, logger.NewChildLogger());
            }

            logger.AddStorageResourceId(storageResource?.ResourceId)
                .AddArchiveStorageResourceId(archiveStorageResource?.ResourceId);

            // Kick off start-compute before returning.
            await ResourceStartManager.StartComputeAsync(
                record.Value,
                computerResource.ResourceId,
                osDiskResource?.ResourceId,
                storageResource?.ResourceId,
                archiveStorageResource?.ResourceId,
                null,
                input.StartEnvironmentParams,
                logger.NewChildLogger());

            // Kick off state transition monitoring.
            await EnvironmentMonitor.MonitorResumeStateTransitionAsync(
                record.Value.Id,
                record.Value.Compute.ResourceId,
                logger.NewChildLogger());
        }

        private async Task QueueResumeEnvironmentAsync(
            EnvironmentResumeActionInput input,
            EnvironmentTransition record,
            EnvironmentResumeTransientState transientState,
            IDiagnosticsLogger logger)
        {
            // Initialize connection, if it is null, client will fail to get environment list.
            record.Value.Connection = new ConnectionInfo();

            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record.Value,
                CloudEnvironmentState.Queued,
                CloudEnvironmentStateUpdateTriggers.StartEnvironment,
                string.Empty,
                null,
                logger.NewChildLogger());

            record.Value.Transitions.ShuttingDown.ResetStatus(true);

            // Set environment state in to transientState object,
            // so that it can be used to suspend/force suspend in case of an exception.
            transientState.CloudEnvironmentState = record.Value.State;

            // Persist core cloud environment record
            var updatedEnvironment = await Repository.UpdateAsync(record.Value, logger.NewChildLogger());
            record.ReplaceAndResetTransition(updatedEnvironment);

            await EnvironmentContinuation.ResumeAsync(
                Guid.Parse(record.Value.Id),
                record.Value.LastStateUpdated,
                input.StartEnvironmentParams,
                "resumeenvironment",
                logger.NewChildLogger());
        }

        private async Task<VsoPlan> FetchPlanAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            var isPlanIdValid = VsoPlanInfo.TryParse(environment.PlanId, out var planInfo);
            ValidationUtil.IsTrue(isPlanIdValid, $"Invalid plan ID: {environment.PlanId}");
            var planDetails = await PlanManager.GetAsync(planInfo, logger);
            ValidationUtil.IsTrue(planDetails != null, $"Plan '{environment.PlanId}' not found.");

            return planDetails;
        }

        private void ValidateInput(EnvironmentResumeActionInput input)
        {
            // Validate input
            ValidationUtil.IsRequired(input, nameof(input));
            ValidationUtil.IsRequired(input.StartEnvironmentParams, nameof(input.StartEnvironmentParams));
        }

        private async Task ValidateEnvironmentAsync(CloudEnvironment environment, VsoPlan plan, IDiagnosticsLogger logger)
        {
            // Static Environment
            if (environment.Type == EnvironmentType.StaticEnvironment)
            {
                throw new CodedValidationException((int)MessageCodes.StartStaticEnvironment);
            }

            // Cannot resume an environment that is not suspended
            if (!environment.IsShutdown())
            {
                throw new CodedValidationException((int)MessageCodes.EnvironmentNotShutdown);
            }

            // Validate sku details
            await ValidateSkuAsync(environment.SkuName, plan.Plan);

            // Validate VNet details
            var isVnetInjectionEnabled = await PlanManager.CheckFeatureFlagsAsync(plan, PlanFeatureFlag.VnetInjection, logger.NewChildLogger());
            ValidationUtil.IsTrue(isVnetInjectionEnabled, "The requested vnet injection feature is disabled.");
        }

        private async Task ValidatePlanAndSubscriptionAsync(CloudEnvironment environment, VsoPlan plan, IDiagnosticsLogger logger)
        {
            SkuCatalog.CloudEnvironmentSkus.TryGetValue(environment.SkuName, out var sku);

            // Validate whether or not the subscription is allowed to create plans and environments.
            var subscription = await SubscriptionManager.GetSubscriptionAsync(plan.Plan.Subscription, logger.NewChildLogger());
            if (!await SubscriptionManager.CanSubscriptionCreatePlansAndEnvironmentsAsync(subscription, logger.NewChildLogger()))
            {
                throw new ForbiddenException((int)MessageCodes.SubscriptionCannotPerformAction);
            }

            // Validate subscription state
            if (subscription.SubscriptionState != SubscriptionStateEnum.Registered)
            {
                logger.LogError($"{LogBaseName}_resume_subscriptionstate_error");
                throw new ForbiddenException((int)MessageCodes.SubscriptionStateIsNotRegistered);
            }

            // Check subscription quota
            var computeCheckEnabled = await EnvironmentManagerSettings.ComputeCheckEnabled(logger.NewChildLogger());
            var windowsComputeCheckEnabled = await EnvironmentManagerSettings.WindowsComputeCheckEnabled(logger.NewChildLogger());
            if (sku.ComputeOS == ComputeOS.Windows)
            {
                computeCheckEnabled = computeCheckEnabled && windowsComputeCheckEnabled;
            }

            if (computeCheckEnabled)
            {
                var reachedComputeLimit = await EnvironmentSubscriptionManager.HasReachedMaxComputeUsedForSubscriptionAsync(subscription, sku, logger.NewChildLogger());
                if (reachedComputeLimit)
                {
                    throw new ForbiddenException((int)MessageCodes.ExceededQuota);
                }
            }
        }

        private Task<ResourceAllocationRecord> AllocateComputeAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            return AllocateResourceAsync(cloudEnvironment, ResourceType.ComputeVM, logger);
        }

        private Task<ResourceAllocationRecord> AllocateStorageAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            return AllocateResourceAsync(cloudEnvironment, ResourceType.StorageFileShare, logger);
        }

        private async Task<ResourceAllocationRecord> AllocateResourceAsync(
            CloudEnvironment cloudEnvironment,
            ResourceType resourceType,
            IDiagnosticsLogger logger)
        {
            var inputRequest = new AllocateRequestBody
            {
                Type = resourceType,
                SkuName = cloudEnvironment.SkuName,
                Location = cloudEnvironment.Location,
            };

            try
            {
                var resultResponse = await ResourceAllocationManager.AllocateResourcesAsync(
                    Guid.Parse(cloudEnvironment.Id),
                    new List<AllocateRequestBody>() { inputRequest },
                    logger.NewChildLogger());

                return resultResponse.Single();
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBaseName}_resume_allocate_error", ex);
                throw new UnavailableException((int)MessageCodes.UnableToAllocateResourcesWhileStarting, ex.Message, ex);
            }
        }
    }
}
