// <copyright file="EnvironmentBaseStartAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Base Start Action for either resuming or exporting an environment. Returns a reference of <see cref="CloudEnvironment"/> a default reult type.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TState">Transitent state to track properties required for exception handling.</typeparam>
    public abstract class EnvironmentBaseStartAction<TInput, TState> : EnvironmentItemAction<TInput, TState>, IEnvironmentBaseStartAction<TInput, TState, CloudEnvironment>
    where TState : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentBaseStartAction{TInput, TState}"/> class.
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
        protected EnvironmentBaseStartAction(
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
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
        }

        /// <summary>
        /// Gets workspace manager.
        /// </summary>
        protected IWorkspaceManager WorkspaceManager { get; }

        /// <summary>
        /// Gets environment monitor.
        /// </summary>
        protected IEnvironmentMonitor EnvironmentMonitor { get; }

        /// <summary>
        /// Gets environment continuation.
        /// </summary>
        protected IEnvironmentContinuationOperations EnvironmentContinuation { get; }

        /// <summary>
        /// Gets resource start manager.
        /// </summary>
        protected IResourceStartManager ResourceStartManager { get; }

        private IPlanManager PlanManager { get; }

        private ISubscriptionManager SubscriptionManager { get; }

        private IEnvironmentSubscriptionManager EnvironmentSubscriptionManager { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IEnvironmentSuspendAction EnvironmentSuspendAction { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        private ITaskHelper TaskHelper { get; }

        /// <summary>
        /// Configures Run Core Async.
        /// </summary>
        /// <param name="record"> Environment transition record. </param>
        /// <param name="input"> Environment start action input. </param>
        /// <param name="transientState"> Environment start action transient state. </param>
        /// <param name="logger"> Logger. </param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected async Task<EnvironmentTransition> ConfigureRunCoreAsync(
            EnvironmentTransition record,
            TInput input,
            EnvironmentStartTransientState transientState,
            IDiagnosticsLogger logger)
        {
            // Redirect early if the Codespace is in the wrong region.
            ValidateTargetLocation(record.Value.Location, logger);

            var plan = await FetchPlanAsync(record.Value, logger.NewChildLogger());

            // Validate
            ValidateInput(input);
            await ValidateEnvironmentAsync(record.Value, plan, logger);
            var subscriptionComputeData = await ValidatePlanAndSubscriptionAsync(record.Value, plan, logger);

            // Authorize
            EnvironmentAccessManager.AuthorizeEnvironmentAccess(record.Value, nonOwnerScopes: null, logger);

            // Add VNet information to environment
            var subnetId = plan.Properties?.VnetProperties?.SubnetId;
            record.PushTransition((environment) =>
            {
                environment.SubnetResourceId = subnetId;
            });

            var connectionWorkspaceRootId = record.Value.Connection?.WorkspaceId;
            if (!string.IsNullOrWhiteSpace(connectionWorkspaceRootId))
            {
                // Delete the previous liveshare session from database.
                // Do not block start process on delete of old workspace from liveshare db.
                TaskHelper.RunBackground(
                    "delete_workspace",
                    (childLogger) => WorkspaceManager.DeleteWorkspaceAsync(connectionWorkspaceRootId, childLogger),
                    logger,
                    true);

                record.PushTransition((environment) =>
                {
                    environment.Connection.ConnectionComputeId = null;
                    environment.Connection.ConnectionComputeTargetId = null;
                    environment.Connection.ConnectionServiceUri = null;
                    environment.Connection.ConnectionSessionId = null;
                    environment.Connection.WorkspaceId = null;
                });
            }

            // Add Subscription quota data
            record.Value.SubscriptionData = new SubscriptionData
            {
                SubscriptionId = plan.Plan.Subscription,
                ComputeUsage = subscriptionComputeData.ComputeUsage,
                ComputeQuota = subscriptionComputeData.ComputeQuota,
            };

            return record;
        }

        /// <summary>
        /// Handles Exceptions for environment.
        /// </summary>
        /// <param name="inputId"> Environment start action input id. </param>
        /// <param name="ex"> Exception caught. </param>
        /// <param name="transientState"> Environment start action transient state.</param>
        /// <param name="expectedState"> Expected state is where the method is being called from (export or resume). </param>
        /// <param name="logger"> Logger. </param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected async Task<bool> HandleExceptionAsync(
           Guid inputId,
           Exception ex,
           EnvironmentStartTransientState transientState,
           CloudEnvironmentState expectedState,
           IDiagnosticsLogger logger)
        {
            var isFullyHandled = false;

            // Suspend the environment if the state is already changed to expected state/queued
            if (transientState.CloudEnvironmentState == expectedState || transientState.CloudEnvironmentState == CloudEnvironmentState.Queued)
            {
                await EnvironmentSuspendAction.RunAsync(inputId, transientState.AllocatedComputeId, logger.NewChildLogger());
            }
            else if (transientState.AllocatedComputeId != default)
            {
                // Delete the allocated resources.
                await ResourceBrokerClient.DeleteAsync(inputId, transientState.AllocatedComputeId, logger.NewChildLogger());
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

        /// <summary>
        /// Starting action for setting state of environment, starting the compute, and kicking off state transition monitoring.
        /// </summary>
        /// <param name="input"> Environment start action input.</param>
        /// <param name="record"> Transition cloud environment record. </param>
        /// <param name="transientState"> Current transient state of environment. </param>
        /// <param name="startAction"> Start action of environment. </param>
        /// <param name="logger"> Logger. </param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        protected async Task StartEnvironmentAsync(
            TInput input,
            EnvironmentTransition record,
            EnvironmentStartTransientState transientState,
            StartEnvironmentAction startAction,
            IDiagnosticsLogger logger)
        {
            // Allocate Compute
            var allocatedCompute = await AllocateComputeAsync(record.Value, logger.NewChildLogger());
            record.PushTransition((environment) =>
            {
                environment.Compute = allocatedCompute;
            });

            // Set compute id in to transientState object,
            // so that it can be used for deallocating in case of an exception.
            transientState.AllocatedComputeId = record.Value.Compute.ResourceId;

            // Start Environment Monitoring
            await EnvironmentMonitor.MonitorHeartbeatAsync(record.Value.Id, record.Value.Compute.ResourceId, logger.NewChildLogger());

            // Create the Live Share workspace only if we are resuming environment
            await AddWorkspaceConnection(record, input, logger);

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

            // Set current state and update trigger based on start action.
            var currentState = (startAction == StartEnvironmentAction.StartCompute) ? CloudEnvironmentState.Starting : CloudEnvironmentState.Exporting;
            var updateTrigger = (startAction == StartEnvironmentAction.StartCompute) ? CloudEnvironmentStateUpdateTriggers.StartEnvironment : CloudEnvironmentStateUpdateTriggers.ExportEnvironment;

            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record,
                currentState,
                updateTrigger,
                startingStateReson,
                null,
                logger.NewChildLogger());

            // Set environment state in to transientState object,
            // so that it can be used to suspend/force suspend in case of an exception.
            transientState.CloudEnvironmentState = record.Value.State;

            record.PushTransition((environment) =>
            {
                environment.Transitions.ShuttingDown.ResetStatus(true);
            });

            // Persist updates made to date
            await Repository.UpdateTransitionAsync("cloudenvironment", record, logger);

            // Provision new storage if environment has been archvied but don't switch until complete
            if (archiveStorageResource != null)
            {
                storageResource = await AllocateStorageAsync(record.Value, logger.NewChildLogger());
            }

            logger.AddStorageResourceId(storageResource?.ResourceId)
                .AddArchiveStorageResourceId(archiveStorageResource?.ResourceId);

            await StartComputeAndMonitor(record, storageResource, archiveStorageResource, input, logger);
        }

        /// <summary>
        /// Queueing start of environment.
        /// </summary>
        /// <param name="input"> Environment start action input.</param>
        /// <param name="record"> Transition cloud environment record. </param>
        /// <param name="transientState"> Current transient state of environment. </param>
        /// <param name="startAction"> Start action of environment.</param>
        /// <param name="logger"> Logger. </param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        protected async Task QueueStartEnvironmentAsync(
            TInput input,
            EnvironmentTransition record,
            EnvironmentStartTransientState transientState,
            StartEnvironmentAction startAction,
            IDiagnosticsLogger logger)
        {
            var updateTrigger = (startAction == StartEnvironmentAction.StartCompute) ? CloudEnvironmentStateUpdateTriggers.StartEnvironment : CloudEnvironmentStateUpdateTriggers.ExportEnvironment;

            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record,
                CloudEnvironmentState.Queued,
                updateTrigger,
                string.Empty,
                null,
                logger);

            record.PushTransition((environment) =>
            {
                // Initialize connection, if it is null, client will fail to get environment list.
                environment.Connection = new ConnectionInfo();

                environment.Transitions.ShuttingDown.ResetStatus(true);
            });

            // Set environment state in to transientState object,
            // so that it can be used to suspend/force suspend in case of an exception.
            transientState.CloudEnvironmentState = record.Value.State;

            // Apply transitions and persist the environment to database
            await Repository.UpdateTransitionAsync("cloudenvironment", record, logger);

            // Run appropriate async method for environment continuation.
            await StartEnvironmentContinuation(record, input, logger);
        }

        /// <summary>
        /// Validate input.
        /// </summary>
        /// <param name="input"> Environment start action input.</param>
        protected abstract void ValidateInput(TInput input);

        /// <summary>
        /// Adding liveshare workspace connection.
        /// </summary>
        /// <param name="record"> Transition cloud environment record. </param>
        /// <param name="input"> Input. </param>
        /// <param name="logger"> Logger. </param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        protected abstract Task<EnvironmentTransition> AddWorkspaceConnection(EnvironmentTransition record, TInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Starts compute and state monitoring.
        /// </summary>
        /// <param name="record"> Transition cloud environment record. </param>
        /// <param name="storageResource"> Storage resource. </param>
        /// <param name="archiveStorageResource"> Archive storage resource. </param>
        /// <param name="input"> Input. </param>
        /// <param name="logger"> Logger. </param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        protected abstract Task StartComputeAndMonitor(EnvironmentTransition record, ResourceAllocationRecord storageResource, ResourceAllocationRecord archiveStorageResource, TInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Starts environment continuation for if action was queued.
        /// </summary>
        /// <param name="record"> Transition cloud environment record. </param>
        /// <param name="input"> Input. </param>
        /// <param name="logger"> Logger. </param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        protected abstract Task StartEnvironmentContinuation(EnvironmentTransition record, TInput input, IDiagnosticsLogger logger);

        private async Task<VsoPlan> FetchPlanAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            var isPlanIdValid = VsoPlanInfo.TryParse(environment.PlanId, out var planInfo);
            ValidationUtil.IsTrue(isPlanIdValid, $"Invalid plan ID: {environment.PlanId}");
            var planDetails = await PlanManager.GetAsync(planInfo, logger);
            ValidationUtil.IsTrue(planDetails != null, $"Plan '{environment.PlanId}' not found.");

            return planDetails;
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

        private async Task<SubscriptionComputeData> ValidatePlanAndSubscriptionAsync(CloudEnvironment environment, VsoPlan plan, IDiagnosticsLogger logger)
        {
            SkuCatalog.CloudEnvironmentSkus.TryGetValue(environment.SkuName, out var sku);
            var subscriptionComputeData = new SubscriptionComputeData();

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

            subscriptionComputeData = await EnvironmentSubscriptionManager.HasReachedMaxComputeUsedForSubscriptionAsync(subscription, sku, logger.NewChildLogger());
            if (computeCheckEnabled && subscriptionComputeData.HasReachedQuota)
            {
                throw new ForbiddenException((int)MessageCodes.ExceededQuota);
            }

            return subscriptionComputeData;
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
                logger.LogException($"{LogBaseName}_start_allocate_error", ex);
                throw new UnavailableException((int)MessageCodes.UnableToAllocateResourcesWhileStarting, ex.Message, ex);
            }
        }
    }
}
