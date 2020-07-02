// <copyright file="EnvironmentStateManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
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
        /// <param name="billingEventManager">target billing manager.</param>
        /// <param name="environmentMetricsLogger">The metrics logger.</param>
        public EnvironmentStateManager(
            IBillingEventManager billingEventManager,
            IEnvironmentMetricsManager environmentMetricsLogger)
        {
            BillingEventManager = billingEventManager;
            EnvironmentMetricsLogger = environmentMetricsLogger;
        }

        private IBillingEventManager BillingEventManager { get; }

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
                    var oldState = cloudEnvironment.State;
                    var oldStateUpdated = cloudEnvironment.LastStateUpdated;

                    string failedStateReason = string.Empty;
                    if (newState == CloudEnvironmentState.Failed)
                    {
                        if (isUserError == true)
                        {
                            failedStateReason = "user";
                        }
                    }

                    logger.FluentAddBaseValue("CloudEnvironmentOldState", oldState)
                        .FluentAddBaseValue("CloudEnvironmentOldStateUpdated", oldStateUpdated)
                        .FluentAddBaseValue("CloudEnvironmentOldStateUpdatedTrigger", cloudEnvironment.LastStateUpdateTrigger)
                        .FluentAddBaseValue("CloudEnvironmentOldStateUpdatedReason", cloudEnvironment.LastStateUpdateReason)
                        .FluentAddBaseValue("CloudEnvironmentNewState", newState)
                        .FluentAddBaseValue("CloudEnvironmentNewUpdatedTrigger", trigger)
                        .FluentAddBaseValue("CloudEnvironmentNewUpdatedReason", reason)
                        .FluentAddBaseValue("CloudEnvironmentFailedStateReason", failedStateReason);

                    VsoPlanInfo plan;
                    if (cloudEnvironment.PlanId == default)
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
                            VsoPlanInfo.TryParse(cloudEnvironment.PlanId, out plan),
                            nameof(cloudEnvironment.PlanId),
                            "Invalid plan ID");

                        plan.Location = cloudEnvironment.Location;
                    }

                    var environment = new EnvironmentBillingInfo
                    {
                        Id = cloudEnvironment.Id,
                        Name = cloudEnvironment.FriendlyName,
                        Sku = new Sku { Name = cloudEnvironment.SkuName, Tier = string.Empty },
                    };

                    var stateChange = new BillingStateChange
                    {
                        OldValue = (oldState == default ? CloudEnvironmentState.Created : oldState).ToString(),
                        NewValue = newState.ToString(),
                    };

                    await BillingEventManager.CreateEventAsync(
                        plan, environment, BillingEventTypes.EnvironmentStateChange, stateChange, logger.NewChildLogger());

                    var stateSnapshot = new CloudEnvironmentStateSnapshot(cloudEnvironment);
                    cloudEnvironment.State = newState;
                    cloudEnvironment.LastStateUpdateTrigger = trigger;
                    cloudEnvironment.LastStateUpdated = DateTime.UtcNow;
                    cloudEnvironment.StateTimeout = null; // reset the state timeout as the transition has now occurred

                    if (reason != null)
                    {
                        cloudEnvironment.LastStateUpdateReason = reason;
                    }

                    // Log to business metrics, post both current state and prior state.
                    EnvironmentMetricsLogger.PostEnvironmentEvent(cloudEnvironment, stateSnapshot, logger.NewChildLogger());

                    // Log to operational telemetry
                    logger.AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogMessage(nameof(SetEnvironmentStateAsync)));
                });
        }
    }
}
