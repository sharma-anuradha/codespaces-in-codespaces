// <copyright file="EnvironmentBaseIntializeStartAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Intialize Start Action.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    public abstract class EnvironmentBaseIntializeStartAction<TInput> : EnvironmentItemAction<TInput, object>, IEnvironmentBaseIntializeStartAction<TInput>
    where TInput : EnvironmentBaseStartActionInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentBaseIntializeStartAction{TInput}"/> class.
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
        protected EnvironmentBaseIntializeStartAction(
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
            EnvironmentManagerSettings environmentManagerSettings)
            : base(
                  environmentStateManager,
                  repository,
                  currentLocationProvider,
                  currentUserProvider,
                  controlPlaneInfo,
                  environmentAccessManager,
                  skuCatalog,
                  skuUtils)
        {
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            SubscriptionManager = Requires.NotNull(subscriptionManager, nameof(subscriptionManager));
            EnvironmentSubscriptionManager = Requires.NotNull(environmentSubscriptionManager, nameof(environmentSubscriptionManager));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
        }

        private IPlanManager PlanManager { get; }

        private ISubscriptionManager SubscriptionManager { get; }

        private IEnvironmentSubscriptionManager EnvironmentSubscriptionManager { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        /// <summary>
        /// Configures Run Core Async.
        /// </summary>
        /// <param name="record"> Environment transition record. </param>
        /// <param name="logger"> Logger. </param>
        /// <returns>True if the action is good to proceed, false otherwise..</returns>
        protected async Task<bool> ConfigureRunCoreAsync(
            EnvironmentTransition record,
            IDiagnosticsLogger logger)
        {
            // No action required if the environment is already running
            if (record.Value.State == CloudEnvironmentState.Available)
            {
                return false;
            }

            // No action required if the environment is already in target state
            if (IsEnvironmentInTargetState(record.Value.State))
            {
                return false;
            }

            var plan = await FetchPlanAsync(record.Value, logger.NewChildLogger());

            // Validate
            await ValidateEnvironmentAsync(record.Value, plan, logger);
            var subscriptionComputeData = await ValidatePlanAndSubscriptionAsync(record.Value, plan, logger);

            // Add Subscription quota data
            var subscriptionData = new SubscriptionData
            {
                SubscriptionId = plan.Plan.Subscription,
                ComputeUsage = subscriptionComputeData.ComputeUsage,
                ComputeQuota = subscriptionComputeData.ComputeQuota,
            };
            record.PushTransition(environment =>
            {
                environment.SubscriptionData = subscriptionData;
            });

            // Authorize
            EnvironmentAccessManager.AuthorizeEnvironmentAccess(record.Value, nonOwnerScopes: null, logger);

            return true;
        }

        /// <inheritdoc/>
        protected override async Task<bool> HandleExceptionAsync(
            TInput input,
            Exception ex,
            object transientState,
            IDiagnosticsLogger logger)
        {
            // Stop if the record in the database is already in target state
            if (ex is ConflictException ce && ce.MessageCode == (int)CommonMessageCodes.ConcurrentModification)
            {
                var record = await FetchAsync(input, logger);
                if (IsEnvironmentInTargetState(record.Value.State))
                {
                    logger.AddReason("Already being started.");
                }
            }

            // No further actions required, return exception to client.
            return false;
        }

        /// <summary>
        /// Update environment state and save the record.
        /// </summary>
        /// <param name="record">Target environment entity transition record.</param>
        /// <param name="targetState">Target state.</param>
        /// <param name="reason">State change reason.</param>
        /// <param name="trigger">State change trigger.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task UpdateStateAsync(
            EnvironmentTransition record,
            CloudEnvironmentState targetState,
            string reason,
            string trigger,
            IDiagnosticsLogger logger)
        {
            await EnvironmentStateManager.SetEnvironmentStateAsync(
                                record,
                                targetState,
                                trigger,
                                reason,
                                null,
                                logger);

            // Apply transitions and persist the environment to database
            await Repository.UpdateTransitionAsync("cloudenvironment", record, logger);
        }

        /// <summary>
        /// Check whether the environment is in target state.
        /// </summary>
        /// <param name="cloudEnvironmentState">Current state.</param>
        /// <returns>True if environment is in target state, false otherwise.</returns>
        protected abstract bool IsEnvironmentInTargetState(CloudEnvironmentState cloudEnvironmentState);

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
            // Cannot start an environment that is not suspended
            if (!environment.IsShutdown())
            {
                throw new CodedValidationException((int)MessageCodes.EnvironmentNotShutdown);
            }

            // Static Environment
            if (environment.Type == EnvironmentType.StaticEnvironment)
            {
                throw new CodedValidationException((int)MessageCodes.StartStaticEnvironment);
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

            var subscriptionComputeData = await EnvironmentSubscriptionManager.HasReachedMaxComputeUsedForSubscriptionAsync(subscription, sku, plan.Partner, logger.NewChildLogger());
            if (computeCheckEnabled && subscriptionComputeData.HasReachedQuota)
            {
                throw new ForbiddenException((int)MessageCodes.ExceededQuota);
            }

            return subscriptionComputeData;
        }
    }
}
