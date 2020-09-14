// <copyright file="EnvironmentCreateAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Create Action.
    /// </summary>
    public class EnvironmentCreateAction : EnvironmentItemAction<EnvironmentCreateActionInput, EnvironmentCreateTransientState>, IEnvironmentCreateAction
    {
        private const string QueueResourceAllocationFeatureFlagKey = "featureflag:queue-resource-request-windows-enabled";
       
        private const bool QueueResourceAllocationDefault = true;
        
        private readonly Regex envNameRegex = new Regex(@"^[-\w\._\(\) ]{1,90}$", RegexOptions.IgnoreCase);
       
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentCreateAction"/> class.
        /// </summary>
        /// <param name="planManager">Target plan manager.</param>
        /// <param name="skuCatalog">Target sku catalog.</param>
        /// <param name="skuUtils">Target skuUtils, to find sku's eligiblity.</param>
        /// <param name="environmentListAction">Target environment list action.</param>
        /// <param name="environmentManagerSettings">Target environment manager settings.</param>
        /// <param name="planManagerSettings">Target plan manager settings.</param>
        /// <param name="workspaceManager">Target workspace manager.</param>
        /// <param name="environmentMonitor">Target environment monitor.</param>
        /// <param name="environmentContinuation">Target environment continuation.</param>
        /// <param name="resourceAllocationManager">Target resource allocation manager.</param>
        /// <param name="resourceStartManager">Target resource start manager.</param>
        /// <param name="resourceSelectorFactory">The resource selector factory.</param>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// <param name="environmentDeleteAction">Target environment delete action.</param>
        /// <param name="mapper">The auto mapper.</param>
        /// <param name="environmentActionValidator">Environment action validator.</param>
        /// <param name="systemConfiguration">System configuration settings.</param>
        public EnvironmentCreateAction(
            IPlanManager planManager,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils,
            IEnvironmentListAction environmentListAction,
            EnvironmentManagerSettings environmentManagerSettings,
            PlanManagerSettings planManagerSettings,
            IWorkspaceManager workspaceManager,
            IEnvironmentMonitor environmentMonitor,
            IEnvironmentContinuationOperations environmentContinuation,
            IResourceAllocationManager resourceAllocationManager,
            IResourceStartManager resourceStartManager,
            IResourceSelectorFactory resourceSelectorFactory,
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            IEnvironmentHardDeleteAction environmentDeleteAction,
            IMapper mapper,
            IEnvironmentActionValidator environmentActionValidator,
            ISystemConfiguration systemConfiguration)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager, skuCatalog, skuUtils)
        {
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            EnvironmentListAction = Requires.NotNull(environmentListAction, nameof(environmentListAction));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
            PlanManagerSettings = Requires.NotNull(planManagerSettings, nameof(planManagerSettings));
            WorkspaceManager = Requires.NotNull(workspaceManager, nameof(workspaceManager));
            EnvironmentMonitor = Requires.NotNull(environmentMonitor, nameof(environmentMonitor));
            EnvironmentContinuation = Requires.NotNull(environmentContinuation, nameof(environmentContinuation));
            ResourceAllocationManager = Requires.NotNull(resourceAllocationManager, nameof(resourceAllocationManager));
            ResourceStartManager = Requires.NotNull(resourceStartManager, nameof(resourceStartManager));
            ResourceSelectorFactory = Requires.NotNull(resourceSelectorFactory, nameof(resourceSelectorFactory));
            EnvironmentDeleteAction = Requires.NotNull(environmentDeleteAction, nameof(environmentDeleteAction));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
            EnvironmentActionValidator = Requires.NotNull(environmentActionValidator, nameof(environmentActionValidator));
            SystemConfiguration = Requires.NotNull(systemConfiguration, nameof(systemConfiguration));
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_create_action";

        private IPlanManager PlanManager { get; }

        private IEnvironmentListAction EnvironmentListAction { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private PlanManagerSettings PlanManagerSettings { get; }

        private IWorkspaceManager WorkspaceManager { get; }

        private IEnvironmentMonitor EnvironmentMonitor { get; }

        private IEnvironmentContinuationOperations EnvironmentContinuation { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IResourceStartManager ResourceStartManager { get; }

        private IResourceSelectorFactory ResourceSelectorFactory { get; }

        private IEnvironmentHardDeleteAction EnvironmentDeleteAction { get; }

        private IMapper Mapper { get; }

        private IEnvironmentActionValidator EnvironmentActionValidator { get; }

        private ISystemConfiguration SystemConfiguration { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> RunAsync(
            EnvironmentCreateDetails details,
            StartCloudEnvironmentParameters startEnvironmentParams,
            MetricsInfo metricsInfo,
            IDiagnosticsLogger logger)
        {
            // Base Validation
            ValidationUtil.IsRequired(details, nameof(details));

            // Default plan id to current user
            details.PlanId ??= CurrentUserProvider.Identity.AuthorizedPlan;

            // Pull Plan
            var isPlanIdValid = VsoPlanInfo.TryParse(details.PlanId, out var plan);
            ValidationUtil.IsTrue(isPlanIdValid, $"Invalid plan ID: {details.PlanId}");
            var planDetails = await PlanManager.GetAsync(plan, logger);
            ValidationUtil.IsTrue(planDetails != null, $"Plan '{details.PlanId}' not found.");

            // Build input
            var input = new EnvironmentCreateActionInput
            {
                Details = details,
                Plan = planDetails,
                MetricsInfo = metricsInfo,
                StartEnvironmentParams = startEnvironmentParams,
            };

            return await RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentCreateActionInput input,
            EnvironmentCreateTransientState transientState,
            IDiagnosticsLogger logger)
        {
            // Core Logging
            logger.AddVsoPlan(input.Plan);

            // Authorize Access
            var requiredScopes = new[]
            {
                PlanAccessTokenScopes.WriteEnvironments,
                PlanAccessTokenScopes.WriteCodespaces,
            };
            EnvironmentAccessManager.AuthorizePlanAccess(input.Plan, requiredScopes, null, logger);

            // Core Validation
            ValidateInput(input, logger);
            ValidateTargetLocation(input.Plan.Plan.Location, logger);
            await ValidateEnvironmentAsync(input, logger);
            var subscriptionComputeData = await ValidateSubscriptionAndPlanAsync(input, logger);

            // Build Transition
            var cloudEnvironment = Mapper.Map<EnvironmentCreateDetails, CloudEnvironment>(input.Details);
            var record = BuildTransition(cloudEnvironment);

            // Map core properties
            record.Value.Id = Guid.NewGuid().ToString();
            record.Value.Location = input.Plan.Plan.Location;
            record.Value.ControlPlaneLocation = ControlPlaneInfo.Stamp.Location;
            record.Value.Partner = input.Plan.Partner;
            record.Value.OwnerId = CurrentUserProvider.CurrentUserIdSet.PreferredUserId;
            record.Value.Created = record.Value.Updated = record.Value.LastUsed = DateTime.UtcNow;
            record.Value.HasUnpushedGitChanges = false;
            record.Value.SubnetResourceId = input.Plan.Properties?.VnetProperties?.SubnetId;
            record.Value.SkuName ??= input.Plan.Properties?.VnetProperties?.SubnetId;

            // Set to transient state to facilitate cleanup in case of environment creation failure
            transientState.EnvironmentId = Guid.Parse(record.Value.Id);

            // Build options
            SkuCatalog.CloudEnvironmentSkus.TryGetValue(record.Value.SkuName, out var sku);
            var queueResourceRequestFlag = await SystemConfiguration.GetValueAsync(QueueResourceAllocationFeatureFlagKey, logger.NewChildLogger(), QueueResourceAllocationDefault);
            var queueResourceRequestForWindows = queueResourceRequestFlag && sku.ComputeOS == ComputeOS.Windows;
            var environmentOptions = new CloudEnvironmentOptions();
            if (input.Details.ExperimentalFeatures != null)
            {
                environmentOptions.CustomContainers = input.Details.ExperimentalFeatures.CustomContainers;
                environmentOptions.NewTerminal = input.Details.ExperimentalFeatures.NewTerminal;
                record.Value.QueueResourceAllocation = input.Details.ExperimentalFeatures.QueueResourceAllocation || queueResourceRequestForWindows;
            }

            logger.FluentAddBaseValue(nameof(record.Value.QueueResourceAllocation), record.Value.QueueResourceAllocation);

            // Setup static environment
            if (record.Value.Type == EnvironmentType.StaticEnvironment)
            {
                await RunCoreStaticEnvironmentAsync(input, record, transientState, logger);
            }
            else
            {
                // Queued or standard create
                if (record.Value.QueueResourceAllocation || !string.IsNullOrEmpty(record.Value.SubnetResourceId))
                {
                    await QueueCoreEnvironmentAsync(input, record, environmentOptions, transientState, logger);
                }
                else
                {
                    await RunCoreEnvironmentAsync(input, record, environmentOptions, transientState, logger);
                }
            }

            // Add Subscription quota data
            record.Value.SubscriptionData = new SubscriptionData
            {
                SubscriptionId = input.Plan.Plan.Subscription,
                ComputeUsage = subscriptionComputeData.ComputeUsage,
                ComputeQuota = subscriptionComputeData.ComputeQuota,
            };

            return record.Value;
        }

        /// <inheritdoc/>
        protected override async Task<bool> HandleExceptionAsync(
            EnvironmentCreateActionInput input,
            Exception ex,
            EnvironmentCreateTransientState transientState,
            IDiagnosticsLogger logger)
        {
            var isFullyHandled = false;

            if (transientState.EnvironmentId != default)
            {
                // Delete the environment
                await EnvironmentDeleteAction.RunAsync(
                    transientState.EnvironmentId,
                    transientState.AllocatedComputeId,
                    transientState.AllocatedStorageId,
                    transientState.AllocatedOsDiskId,
                    transientState.AllocatedLiveshareWorkspaceId,
                    logger.NewChildLogger());
            }

            if (ex is EnvironmentMonitorInitializationException)
            {
                throw new UnavailableException((int)MessageCodes.UnableToAllocateResources, ex.Message, ex);
            }

            // If the code made this far, the exception is not fully handled.
            return isFullyHandled;
        }

        private void ValidateInput(
            EnvironmentCreateActionInput input, IDiagnosticsLogger logger)
        {
            // Base Validation
            ValidationUtil.IsRequired(input, nameof(input));
            ValidationUtil.IsRequired(input.Plan, nameof(input.Plan));
            ValidationUtil.IsRequired(input.Plan.Plan.Location, nameof(input.Plan.Plan.Location));
            ValidationUtil.IsRequired(input.Plan.Plan.ResourceId, "PlanId");
            ValidationUtil.IsRequired(input.StartEnvironmentParams, nameof(input.StartEnvironmentParams));
            ValidationUtil.IsRequired(input.Details, nameof(input.Details));
            ValidationUtil.IsRequired(input.Details.FriendlyName, nameof(input.Details.FriendlyName));
            ValidationUtil.IsRequired(input.Details.SkuName, nameof(input.Details.SkuName));
            ValidationUtil.IsRequired(input.Details.PlanId, nameof(input.Details.PlanId));
            ValidationUtil.IsRequired(input.Details.Type, nameof(input.Details.Type));
            ValidationUtil.IsRequired(input.Details.SkuName, nameof(input.Details.SkuName));
        }

        private async Task ValidateEnvironmentAsync(
            EnvironmentCreateActionInput input, IDiagnosticsLogger logger)
        {
            // Validate name
            input.Details.FriendlyName = input.Details.FriendlyName.Trim();
            ValidationUtil.IsTrue(this.envNameRegex.IsMatch(input.Details.FriendlyName), $"'{input.Details.FriendlyName.Truncate(200)}' is not a valid FriendlyName.");

            // Validate sku details
            await ValidateSkuAsync(input.Details.SkuName, input.Plan.Plan);

            // Validate VNet details
            var isVnetInjectionEnabled = await PlanManager.CheckFeatureFlagsAsync(input.Plan, PlanFeatureFlag.VnetInjection, logger.NewChildLogger());
            ValidationUtil.IsTrue(isVnetInjectionEnabled, "The requested vnet injection feature is disabled.");
        }

        private async Task<SubscriptionComputeData> ValidateSubscriptionAndPlanAsync(
            EnvironmentCreateActionInput input, IDiagnosticsLogger logger)
        {
            SkuCatalog.CloudEnvironmentSkus.TryGetValue(input.Details.SkuName, out var sku);

            // Validate against existing environments
            var environmentsInPlan = await EnvironmentListAction.RunAsync(
                input.Details.PlanId, name: null, identity: null, userIdSet: null, EnvironmentListType.ActiveEnvironments, logger.NewChildLogger());
            if (environmentsInPlan.Any((env) => string.Equals(env.FriendlyName, input.Details.FriendlyName, StringComparison.InvariantCultureIgnoreCase)))
            {
                // TODO: elpadann - when multiple users can access a plan, this should include an ownership check
                throw new ConflictException((int)MessageCodes.EnvironmentNameAlreadyExists);
            }

            var subscriptionComputeData = await EnvironmentActionValidator.ValidateSubscriptionAndQuotaAsync(input.Details.SkuName, environmentsInPlan, input.Plan.Plan.Subscription, input.Plan.Partner, logger.NewChildLogger());

            // Validate suspend timeout
            if (input.Details.Type != EnvironmentType.StaticEnvironment)
            {
                var isValidTimeout = PlanManagerSettings.DefaultAutoSuspendDelayMinutesOptions.Contains(input.Details.AutoShutdownDelayMinutes);
                ValidationUtil.IsTrue(isValidTimeout, $"'{input.Details.AutoShutdownDelayMinutes}' is not a valid AutoShutdownDelayMinutes. Valid options are:  {string.Join(',', PlanManagerSettings.DefaultAutoSuspendDelayMinutesOptions)}");
            }

            return subscriptionComputeData;
        }

        private async Task RunCoreStaticEnvironmentAsync(
            EnvironmentCreateActionInput input,
            EnvironmentTransition record,
            EnvironmentCreateTransientState transientState,
            IDiagnosticsLogger logger)
        {
            // Map core properties
            record.Value.SkuName = StaticEnvironmentSku.Name;
            record.Value.Seed = new SeedInfo { SeedType = SeedType.StaticEnvironment };

            // Create the Live Share workspace
            record.Value.Connection = await WorkspaceManager.CreateWorkspaceAsync(
                EnvironmentType.StaticEnvironment,
                record.Value.Id,
                Guid.Empty,
                input.StartEnvironmentParams.ConnectionServiceUri,
                record.Value.Connection.ConnectionSessionPath,
                input.StartEnvironmentParams.UserProfile.Email,
                input.StartEnvironmentParams.UserProfile.Id,
                record.Value.Partner == Partner.GitHub,
                null,
                logger.NewChildLogger());

            // Set to transient state to facilitate cleanup in case of environment creation failure
            transientState.AllocatedLiveshareWorkspaceId = record.Value.Connection.WorkspaceId;

            // Environments must be initialized in Created state.
            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record.Value, CloudEnvironmentState.Created, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, null, logger.NewChildLogger());

            // But (at least for now) new environments immediately transition to Provisioning state.
            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record.Value, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, null, logger.NewChildLogger());

            // Create new environment record
            var newRecordValue = await Repository.CreateAsync(record.Value, logger.NewChildLogger());
            record.ReplaceAndResetTransition(newRecordValue);

            var staticEnvironmentMonitoringEnabled = await EnvironmentManagerSettings.StaticEnvironmentMonitoringEnabled(logger);
            if (staticEnvironmentMonitoringEnabled)
            {
                await EnvironmentMonitor.MonitorHeartbeatAsync(record.Value.Id, default(Guid), logger.NewChildLogger());
            }
        }

        private async Task QueueCoreEnvironmentAsync(
            EnvironmentCreateActionInput input,
            EnvironmentTransition record,
            CloudEnvironmentOptions environmentOptions,
            EnvironmentCreateTransientState transientState,
            IDiagnosticsLogger logger)
        {
            // Environments must be initialized in Queued state.
            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record.Value, CloudEnvironmentState.Queued, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, null, logger.NewChildLogger());

            // Create new environment record
            var newRecordValue = await Repository.CreateAsync(record.Value, logger.NewChildLogger());
            record.ReplaceAndResetTransition(newRecordValue);

            // Trigger create continuation
            await EnvironmentContinuation.CreateAsync(Guid.Parse(record.Value.Id), record.Value.LastStateUpdated, environmentOptions, input.StartEnvironmentParams, "createnewenvironment", logger.NewChildLogger());
        }

        private async Task RunCoreEnvironmentAsync(
            EnvironmentCreateActionInput input,
            EnvironmentTransition record,
            CloudEnvironmentOptions environmentOptions,
            EnvironmentCreateTransientState transientState,
            IDiagnosticsLogger logger)
        {
            // Allocate Storage and Compute
            var allocationResult = await AllocateComputeAndStorageAsync(record.Value, environmentOptions, logger.NewChildLogger());
            record.Value.Storage = allocationResult.Storage;
            record.Value.Compute = allocationResult.Compute;
            record.Value.OSDisk = allocationResult.OSDisk;

            // Set to transient state to facilitate cleanup in case of environment creation failure
            transientState.AllocatedStorageId = record.Value.Storage?.ResourceId;
            transientState.AllocatedComputeId = record.Value.Compute?.ResourceId;
            transientState.AllocatedOsDiskId = record.Value.OSDisk?.ResourceId;

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
                input.StartEnvironmentParams.UserProfile.Id,
                record.Value.Partner == Partner.GitHub,
                null,
                logger.NewChildLogger());

            // Set to transient state to facilitate cleanup in case of environment creation failure
            transientState.AllocatedLiveshareWorkspaceId = record.Value.Connection.WorkspaceId;

            // Environments must be initialized in Created state.
            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record.Value, CloudEnvironmentState.Created, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, null, logger.NewChildLogger());

            // But (at least for now) new environments immediately transition to Provisioning state.
            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record.Value, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, null, logger.NewChildLogger());

            // Persist core cloud environment record
            var newRecordValue = await Repository.CreateAsync(record.Value, logger.NewChildLogger());
            record.ReplaceAndResetTransition(newRecordValue);

            // Kick off start-compute before returning.
            await ResourceStartManager.StartComputeAsync(
                record.Value, record.Value.Compute.ResourceId, record.Value.OSDisk?.ResourceId, record.Value.Storage?.ResourceId, null, environmentOptions, input.StartEnvironmentParams, StartEnvironmentAction.StartCompute, logger.NewChildLogger());

            // Kick off state transition monitoring.
            await EnvironmentMonitor.MonitorProvisioningStateTransitionAsync(record.Value.Id, record.Value.Compute.ResourceId, logger);
        }

        private async Task<ResourceAllocationResult> AllocateComputeAndStorageAsync(
           CloudEnvironment cloudEnvironment,
           CloudEnvironmentOptions environmentOptions,
           IDiagnosticsLogger logger)
        {
            var inputRequest = await ResourceSelectorFactory.CreateAllocationRequestsAsync(cloudEnvironment, logger);

            try
            {
                var resultResponse = await ResourceAllocationManager.AllocateResourcesAsync(
                    Guid.Parse(cloudEnvironment.Id),
                    inputRequest,
                    logger.NewChildLogger());

                var resourceAllocationResult = new ResourceAllocationResult()
                {
                    Compute = resultResponse.SingleOrDefault(x => x.Type == ResourceType.ComputeVM),
                    Storage = resultResponse.SingleOrDefault(x => x.Type == ResourceType.StorageFileShare),
                    OSDisk = resultResponse.SingleOrDefault(x => x.Type == ResourceType.OSDisk),
                };

                return resourceAllocationResult;
            }
            catch (Exception ex)
            {
                logger.LogError($"{LogBaseName}_create_allocate_error");
                throw new UnavailableException((int)MessageCodes.UnableToAllocateResources, ex.Message, ex);
            }
        }
    }
}
