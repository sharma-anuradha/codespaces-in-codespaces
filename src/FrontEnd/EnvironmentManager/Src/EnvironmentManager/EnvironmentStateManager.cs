// <copyright file="EnvironmentStateManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment State Manager.
    /// </summary>
    public class EnvironmentStateManager : IEnvironmentStateManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentStateManager"/> class.
        /// </summary>
        /// <param name="billingEventManager">target billing manager.</param>
        public EnvironmentStateManager(IBillingEventManager billingEventManager)
        {
            this.BillingEventManager = billingEventManager;
        }

        private IBillingEventManager BillingEventManager { get; }

        /// <inheritdoc/>
        public async Task SetEnvironmentStateAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentState state,
            string trigger,
            string reason,
            IDiagnosticsLogger logger)
        {
            var oldState = cloudEnvironment.State;
            var oldStateUpdated = cloudEnvironment.LastStateUpdated;

            logger.FluentAddBaseValue("OldState", oldState)
                .FluentAddBaseValue("OldStateUpdated", oldStateUpdated);

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
                UserId = cloudEnvironment.OwnerId,
                Sku = new Sku { Name = cloudEnvironment.SkuName, Tier = string.Empty },
            };

            var stateChange = new BillingStateChange
            {
                OldValue = (oldState == default ? CloudEnvironmentState.Created : oldState).ToString(),
                NewValue = state.ToString(),
            };

            await BillingEventManager.CreateEventAsync(
                plan, environment, BillingEventTypes.EnvironmentStateChange, stateChange, logger.NewChildLogger());

            cloudEnvironment.State = state;
            cloudEnvironment.LastStateUpdateTrigger = trigger;
            cloudEnvironment.LastStateUpdated = DateTime.UtcNow;

            if (reason != null)
            {
                cloudEnvironment.LastStateUpdateReason = reason;
            }

            logger.AddCloudEnvironment(cloudEnvironment)
                 .LogInfo(GetType().FormatLogMessage(nameof(SetEnvironmentStateAsync)));
        }
    }
}
