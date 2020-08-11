// <copyright file="EnvironmentStateManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment State Manager.
    /// </summary>
    public class EnvironmentStateManager : IEnvironmentStateManager
    {
        private const string LogBaseName = "environment_state_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentStateManager"/> class.
        /// </summary>
        /// <param name="workspaceManager">Target workspace manager.</param>
        /// <param name="cloudEnvironmentRepository">Target cloud environment repository.</param>
        /// <param name="billingEventManager">target billing manager.</param>
        /// <param name="environmentStateChangeManager">the environment state change manager.</param>
        /// <param name="environmentMetricsLogger">The metrics logger.</param>
        public EnvironmentStateManager(
            IWorkspaceManager workspaceManager,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IBillingEventManager billingEventManager,
            IEnvironmentStateChangeManager environmentStateChangeManager,
            IEnvironmentMetricsManager environmentMetricsLogger)
        {
            WorkspaceManager = workspaceManager;
            CloudEnvironmentRepository = cloudEnvironmentRepository;
            BillingEventManager = billingEventManager;
            EnvironmentStateChangeManager = environmentStateChangeManager;
            EnvironmentMetricsLogger = environmentMetricsLogger;
        }

        private IWorkspaceManager WorkspaceManager { get; }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IBillingEventManager BillingEventManager { get; }

        private IEnvironmentStateChangeManager EnvironmentStateChangeManager { get; }

        private IPlanManager PlanManager { get; }

        private IEnvironmentMetricsManager EnvironmentMetricsLogger { get; }

        /// <inheritdoc/>
        public Task SetEnvironmentStateAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentState newState,
            string trigger,
            string reason,
            bool? isUserError,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_set",
                async (childLogger) =>
                {
                    var record = new EnvironmentTransition(cloudEnvironment);
                    await SetEnvironmentStateAsync(
                        record,
                        newState,
                        trigger,
                        reason,
                        isUserError,
                        childLogger);
                });
        }

        /// <inheritdoc/>
        public Task SetEnvironmentStateAsync(
            EnvironmentTransition record,
            CloudEnvironmentState newState,
            string trigger,
            string reason,
            bool? isUserError,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_set_with_transition",
                async (childLogger) =>
                {
                    var oldState = record.Value.State;
                    var oldStateUpdated = record.Value.LastStateUpdated;
                    string failedStateReason = string.Empty;
                    if (newState == CloudEnvironmentState.Failed)
                    {
                        if (isUserError == true)
                        {
                            failedStateReason = "user";
                        }
                    }

                    // Setup telemetry properties
                    logger.AddCloudEnvironment(record.Value)
                        .FluentAddBaseValue("CloudEnvironmentOldState", oldState)
                        .FluentAddBaseValue("CloudEnvironmentOldStateUpdated", oldStateUpdated)
                        .FluentAddBaseValue("CloudEnvironmentOldStateUpdatedTrigger", record.Value.LastStateUpdateTrigger)
                        .FluentAddBaseValue("CloudEnvironmentOldStateUpdatedReason", record.Value.LastStateUpdateReason)
                        .FluentAddBaseValue("CloudEnvironmentNewState", newState)
                        .FluentAddBaseValue("CloudEnvironmentNewUpdatedTrigger", trigger)
                        .FluentAddBaseValue("CloudEnvironmentNewUpdatedReason", reason)
                        .FluentAddBaseValue("CloudEnvironmentFailedStateReason", failedStateReason);

                    // Get plan information
                    VsoPlanInfo plan;
                    if (record.Value.PlanId == default)
                    {
                        // Use a temporary plan if the environment doesn't have one.
                        // TODO: Remove this; make the plan required after clients are updated to supply it.
                        plan = new VsoPlanInfo
                        {
                            Subscription = Guid.Empty.ToString(),
                            ResourceGroup = "none",
                            Name = "none",
                        };
                    }
                    else
                    {
                        Requires.Argument(
                            VsoPlanInfo.TryParse(record.Value.PlanId, out plan), nameof(record.Value.PlanId), "Invalid plan ID");

                        plan.Location = record.Value.Location;
                    }

                    // Create billing event (legacy)
                    var environment = new EnvironmentBillingInfo
                    {
                        Id = record.Value.Id,
                        Name = record.Value.FriendlyName,
                        Sku = new Sku { Name = record.Value.SkuName, Tier = string.Empty },
                    };
                    var stateChange = new BillingStateChange
                    {
                        OldValue = (oldState == default ? CloudEnvironmentState.Created : oldState).ToString(),
                        NewValue = newState.ToString(),
                    };
                    await BillingEventManager.CreateEventAsync(
                        plan, environment, BillingEventTypes.EnvironmentStateChange, stateChange, logger.NewChildLogger());

                    // Create the new billing state change
                    await EnvironmentStateChangeManager.CreateAsync(plan, environment, oldState, newState, logger.NewChildLogger());

                    // Mutates environment state
                    var lastStateUpdated = DateTime.UtcNow;
                    record.PushTransition((environment) =>
                    {
                        environment.State = newState;
                        environment.LastStateUpdateTrigger = trigger;
                        environment.LastStateUpdated = lastStateUpdated;
                        environment.StateTimeout = null; // reset the state timeout as the transition has now occurred

                        if (reason != null)
                        {
                            environment.LastStateUpdateReason = reason;
                        }
                    });

                    // Posts metrics event
                    var stateSnapshot = new CloudEnvironmentStateSnapshot(oldState, oldStateUpdated);
                    EnvironmentMetricsLogger.PostEnvironmentEvent(record.Value, stateSnapshot, logger.NewChildLogger());

                    // Log to operational telemetry (Do not alter - used by dashboards)
                    logger.AddCloudEnvironment(record.Value)
                        .LogInfo(GetType().FormatLogMessage(nameof(SetEnvironmentStateAsync)));
                });
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> NormalizeEnvironmentStateAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            // TODO: Need to switch over to be Entity Transition based.
            // TODO: Remove once Anu's update tracking is in.
            return logger.OperationScopeAsync(
                $"{LogBaseName}_normalize",
                async (childLogger) =>
                {
                    var originalState = cloudEnvironment.State;
                    var newState = originalState;

                    logger.FluentAddBaseValue("CloudEnvironmentOldState", originalState)
                        .FluentAddValue("CloudEnvironmentOldStateUpdated", cloudEnvironment.LastStateUpdated)
                        .FluentAddValue("CloudEnvironmentOldStateUpdatedTrigger", cloudEnvironment.LastStateUpdateTrigger)
                        .FluentAddValue("CloudEnvironmentOldStateUpdatedReason", cloudEnvironment.LastStateUpdateReason);

                    // Switch on target states
                    switch (originalState)
                    {
                        // Remain in provisioning state until _callback is invoked.
                        case CloudEnvironmentState.Provisioning:

                            // Timeout if environment has stayed in provisioning state for more than an hour
                            var timeInProvisioningStateInMin = (DateTime.UtcNow - cloudEnvironment.LastStateUpdated).TotalMinutes;
                            var timeOverLimit = timeInProvisioningStateInMin > 60;

                            logger.FluentAddBaseValue("CloudEnvironmentTimeInProvisioningStateInMin", timeInProvisioningStateInMin)
                                .FluentAddBaseValue("CloudEnvironmentTimeOverLimit", timeOverLimit);

                            if (timeOverLimit)
                            {
                                newState = CloudEnvironmentState.Failed;

                                childLogger.NewChildLogger()
                                    .LogErrorWithDetail($"{LogBaseName}_normalize_error", $"Marking environment creation failed with timeout. Time in provisioning state {timeInProvisioningStateInMin} minutes.");
                            }

                            break;

                        // Swap between available and awaiting based on the workspace status
                        case CloudEnvironmentState.Available:
                        case CloudEnvironmentState.Awaiting:
                            var sessionId = cloudEnvironment.Connection?.ConnectionSessionId;
                            var workspace = await WorkspaceManager.GetWorkspaceStatusAsync(sessionId, childLogger.NewChildLogger());

                            logger.FluentAddBaseValue("CloudEnvironmentWorkspaceSet", workspace != null)
                                .FluentAddBaseValue("CloudEnvironmentIsHostConnectedHasValue", workspace?.IsHostConnected.HasValue)
                                .FluentAddBaseValue("CloudEnvironmentIsHostConnectedValue", workspace?.IsHostConnected);

                            if (workspace == null)
                            {
                                // In this case the workspace is deleted. There is no way of getting to an environment without it.
                                newState = CloudEnvironmentState.Unavailable;
                            }
                            else if (workspace.IsHostConnected.HasValue)
                            {
                                newState = workspace.IsHostConnected.Value ? CloudEnvironmentState.Available : CloudEnvironmentState.Awaiting;
                            }

                            break;
                    }

                    logger.FluentAddBaseValue("CloudEnvironmentNewState", newState);

                    // Update the new state before returning.
                    if (originalState != newState)
                    {
                        await SetEnvironmentStateAsync(cloudEnvironment, newState, CloudEnvironmentStateUpdateTriggers.GetEnvironment, null, null, childLogger.NewChildLogger());

                        cloudEnvironment.Updated = DateTime.UtcNow;

                        cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());
                    }

                    return cloudEnvironment;
                });
        }
    }
}
