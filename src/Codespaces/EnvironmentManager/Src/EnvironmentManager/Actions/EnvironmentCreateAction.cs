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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
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
    public class EnvironmentCreateAction : EnvironmentItemAction<EnvironmentCreateActionInput>, IEnvironmentCreateAction
    {
        private readonly Regex envNameRegex = new Regex(@"^[-\w\._\(\) ]{1,90}$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentCreateAction"/> class.
        /// </summary>
        /// <param name="planManager">Target plan manager.</param>
        /// <param name="skuCatalog">The sku catalog.</param>
        /// <param name="skuUtils">skuUtils to find sku's eligiblity.</param>
        /// <param name="environmentListAction">Target environment list action.</param>
        /// <param name="environmentManagerSettings">Target environment manager settings.</param>
        /// <param name="planManagerSettings">Target plan manager settings.</param>
        /// <param name="workspaceManager">Target workspace manager.</param>
        /// <param name="environmentMonitor">Target environment monitor.</param>
        /// <param name="environmentContinuation">Target environment continuation.</param>
        /// <param name="resourceAllocationManager">Target resource allocation manager.</param>
        /// <param name="resourceStartManager">Target resource start manager.</param>
        /// <param name="subscriptionManager">Target subscription Manager.</param>
        /// <param name="environmentSubscriptionManager">Target Environment Subscription Manager.</param>
        /// <param name="resourceSelectorFactory">The resource selector factory.</param>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// <param name="environmentDeleteAction">Target environment delete action.</param>
        /// <param name="mapper">The auto mapper.</param>
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
            ISubscriptionManager subscriptionManager,
            IResourceSelectorFactory resourceSelectorFactory,
            IEnvironmentSubscriptionManager environmentSubscriptionManager,
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            IEnvironmentDeleteAction environmentDeleteAction,
            IMapper mapper)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager)
        {
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            SkuUtils = Requires.NotNull(skuUtils, nameof(skuUtils));
            EnvironmentListAction = Requires.NotNull(environmentListAction, nameof(environmentListAction));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
            PlanManagerSettings = Requires.NotNull(planManagerSettings, nameof(planManagerSettings));
            WorkspaceManager = Requires.NotNull(workspaceManager, nameof(workspaceManager));
            EnvironmentMonitor = Requires.NotNull(environmentMonitor, nameof(environmentMonitor));
            EnvironmentContinuation = Requires.NotNull(environmentContinuation, nameof(environmentContinuation));
            ResourceAllocationManager = Requires.NotNull(resourceAllocationManager, nameof(resourceAllocationManager));
            ResourceStartManager = Requires.NotNull(resourceStartManager, nameof(resourceStartManager));
            SubscriptionManager = Requires.NotNull(subscriptionManager, nameof(subscriptionManager));
            ResourceSelectorFactory = Requires.NotNull(resourceSelectorFactory, nameof(resourceSelectorFactory));
            EnvironmentSubscriptionManager = Requires.NotNull(environmentSubscriptionManager, nameof(environmentSubscriptionManager));
            EnvironmentDeleteAction = Requires.NotNull(environmentDeleteAction, nameof(environmentDeleteAction));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_create_action";

        private IPlanManager PlanManager { get; }

        private ISkuCatalog SkuCatalog { get; }

        private ISkuUtils SkuUtils { get; }

        private IEnvironmentListAction EnvironmentListAction { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private PlanManagerSettings PlanManagerSettings { get; }

        private IWorkspaceManager WorkspaceManager { get; }

        private IEnvironmentMonitor EnvironmentMonitor { get; }

        private IEnvironmentContinuationOperations EnvironmentContinuation { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IResourceStartManager ResourceStartManager { get; }

        private ISubscriptionManager SubscriptionManager { get; }

        private IResourceSelectorFactory ResourceSelectorFactory { get; }

        private IEnvironmentSubscriptionManager EnvironmentSubscriptionManager { get; }

        private IEnvironmentDeleteAction EnvironmentDeleteAction { get; }

        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> Run(EnvironmentCreateDetails details, StartCloudEnvironmentParameters startEnvironmentParams, MetricsInfo metricsInfo, IDiagnosticsLogger logger)
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

            return await Run(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(EnvironmentCreateActionInput input, IDiagnosticsLogger logger)
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
            await ValidateSubscriptionAndPlanAsync(input, logger);

            // Build Transition
            var cloudEnvironment = Mapper.Map<EnvironmentCreateDetails, CloudEnvironment>(input.Details);
            var record = BuildTransition(cloudEnvironment);

            // Updating cloudEnvironment record on the input so that HandleExceptionAsync can access it.
            input.CloudEnvironment = record.Value;

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

            // Build options
            var environmentOptions = new CloudEnvironmentOptions();
            if (input.Details.ExperimentalFeatures != null)
            {
                environmentOptions.CustomContainers = input.Details.ExperimentalFeatures.CustomContainers;
                environmentOptions.NewTerminal = input.Details.ExperimentalFeatures.NewTerminal;
                environmentOptions.QueueResourceAllocation = input.Details.ExperimentalFeatures.QueueResourceAllocation;
            }

            // Setup static environment
            if (record.Value.Type == EnvironmentType.StaticEnvironment)
            {
                await RunCoreStaticEnvironmentAsync(input, record, logger);
            }
            else
            {
                // Queued or standard create
                if (environmentOptions.QueueResourceAllocation || !string.IsNullOrEmpty(record.Value.SubnetResourceId))
                {
                    await QueueCoreEnvironmentAsync(input, record, environmentOptions, logger);
                }
                else
                {
                    await RunCoreEnvironmentAsync(input, record, environmentOptions, logger);
                }
            }

            return record.Value;
        }

        /// <inheritdoc/>
        protected override async Task<bool> HandleExceptionAsync(EnvironmentCreateActionInput input, Exception ex, IDiagnosticsLogger logger)
        {
            var isFullyHandled = false;

            if (input.CloudEnvironment != null)
            {
                // Delete the environment
                await EnvironmentDeleteAction.Run(input.CloudEnvironment, logger.NewChildLogger());
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
            SkuCatalog.CloudEnvironmentSkus.TryGetValue(input.Details.SkuName, out var sku);

            // Validate name
            input.Details.FriendlyName = input.Details.FriendlyName.Trim();
            ValidationUtil.IsTrue(this.envNameRegex.IsMatch(input.Details.FriendlyName), $"'{Truncate(input.Details.FriendlyName, 200)}' is not a valid FriendlyName.");

            // Validate sku details
            ValidationUtil.IsTrue(sku != null, $"The requested SKU is not defined: {Truncate(input.Details.SkuName, 200)}");
            var profile = await CurrentUserProvider.GetProfileAsync();
            var isSkuVisible = await SkuUtils.IsVisible(sku, input.Plan.Plan, profile);
            ValidationUtil.IsTrue(isSkuVisible, $"The requested SKU '{Truncate(input.Details.SkuName, 200)}' is not visible.");
            ValidationUtil.IsTrue(sku.Enabled, $"The requested SKU '{Truncate(input.Details.SkuName, 200)}' is not available.");
            ValidationUtil.IsTrue(sku.SkuLocations.Contains(input.Plan.Plan.Location), $"The requested SKU '{Truncate(input.Details.SkuName, 200)}' is not available in location: {input.Plan.Plan.Location}");

            // Validate VNet details
            var isVnetInjectionEnabled = await PlanManager.CheckFeatureFlagsAsync(input.Plan, PlanFeatureFlag.VnetInjection, logger.NewChildLogger());
            ValidationUtil.IsTrue(isVnetInjectionEnabled, "The requested vnet injection feature is disabled.");
        }

        private async Task ValidateSubscriptionAndPlanAsync(
            EnvironmentCreateActionInput input, IDiagnosticsLogger logger)
        {
            SkuCatalog.CloudEnvironmentSkus.TryGetValue(input.Details.SkuName, out var sku);

            // Validate against existing environments
            var environmentsInPlan = await EnvironmentListAction.Run(input.Details.PlanId, null, null, logger.NewChildLogger());
            if (environmentsInPlan.Any((env) => string.Equals(env.FriendlyName, input.Details.FriendlyName, StringComparison.InvariantCultureIgnoreCase)))
            {
                // TODO: elpadann - when multiple users can access a plan, this should include an ownership check
                throw new ConflictException((int)MessageCodes.EnvironmentNameAlreadyExists);
            }

            // Validate environment quota
            var computeCheckEnabled = await EnvironmentManagerSettings.ComputeCheckEnabled(logger.NewChildLogger());
            var windowsComputeCheckEnabled = await EnvironmentManagerSettings.WindowsComputeCheckEnabled(logger.NewChildLogger());
            if (sku.ComputeOS == ComputeOS.Windows)
            {
                computeCheckEnabled = computeCheckEnabled && windowsComputeCheckEnabled;
            }

            var countOfEnvironmentsInPlan = environmentsInPlan.Count();
            var maxEnvironmentsForPlan = await EnvironmentManagerSettings.MaxEnvironmentsPerPlanAsync(input.Plan.Plan.Subscription, logger.NewChildLogger());
            if (!computeCheckEnabled && countOfEnvironmentsInPlan >= maxEnvironmentsForPlan)
            {
                throw new ForbiddenException((int)MessageCodes.ExceededQuota);
            }

            // Check invalid subscription
            var subscription = await SubscriptionManager.GetSubscriptionAsync(input.Plan.Plan.Subscription, logger.NewChildLogger());
            if (!await SubscriptionManager.CanSubscriptionCreatePlansAndEnvironmentsAsync(subscription, logger.NewChildLogger()))
            {
                throw new ForbiddenException((int)MessageCodes.SubscriptionCannotPerformAction);
            }

            // Check banned subscription
            if (subscription.IsBanned)
            {
                throw new ForbiddenException((int)MessageCodes.SubscriptionIsBanned);
            }

            // Check subscription quota
            var reachedComputeLimit = await EnvironmentSubscriptionManager.HasReachedMaxComputeUsedForSubscriptionAsync(subscription, sku, logger.NewChildLogger());
            if (computeCheckEnabled && reachedComputeLimit)
            {
                throw new ForbiddenException((int)MessageCodes.ExceededQuota);
            }

            // Validate suspend timeout
            if (input.Details.Type != EnvironmentType.StaticEnvironment)
            {
                var isValidTimeout = PlanManagerSettings.DefaultAutoSuspendDelayMinutesOptions.Contains(input.Details.AutoShutdownDelayMinutes);
                ValidationUtil.IsTrue(isValidTimeout, $"'{input.Details.AutoShutdownDelayMinutes}' is not a valid AutoShutdownDelayMinutes. Valid options are:  {string.Join(',', PlanManagerSettings.DefaultAutoSuspendDelayMinutesOptions)}");
            }
        }

        private async Task RunCoreStaticEnvironmentAsync(EnvironmentCreateActionInput input, EnvironmentTransition record, IDiagnosticsLogger logger)
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
                null,
                logger.NewChildLogger());

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

        private async Task QueueCoreEnvironmentAsync(EnvironmentCreateActionInput input, EnvironmentTransition record, CloudEnvironmentOptions environmentOptions, IDiagnosticsLogger logger)
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

        private async Task RunCoreEnvironmentAsync(EnvironmentCreateActionInput input, EnvironmentTransition record, CloudEnvironmentOptions environmentOptions, IDiagnosticsLogger logger)
        {
            // Allocate Storage and Compute
            var allocationResult = await AllocateComputeAndStorageAsync(record.Value, environmentOptions, logger.NewChildLogger());
            record.Value.Storage = allocationResult.Storage;
            record.Value.Compute = allocationResult.Compute;
            record.Value.OSDisk = allocationResult.OSDisk;

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
                record.Value, record.Value.Compute.ResourceId, record.Value.OSDisk?.ResourceId, record.Value.Storage?.ResourceId, null, environmentOptions, input.StartEnvironmentParams, logger.NewChildLogger());

            // Kick off state transition monitoring.
            await EnvironmentMonitor.MonitorProvisioningStateTransitionAsync(record.Value.Id, record.Value.Compute.ResourceId, logger);
        }

        private async Task<ResourceAllocationResult> AllocateComputeAndStorageAsync(
           CloudEnvironment cloudEnvironment,
           CloudEnvironmentOptions environmentOptions,
           IDiagnosticsLogger logger)
        {
            var inputRequest = await ResourceSelectorFactory.CreateAllocationRequestsAsync(cloudEnvironment, environmentOptions, logger);

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

        private string Truncate(string value, int maxChars)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value.Length <= maxChars)
            {
                return value;
            }

            return $"{value.Substring(0, maxChars)}...";
        }
    }
}
