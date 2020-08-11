// <copyright file="EnvironmentStateChangeManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// The environment State change manager.
    /// </summary>
    public class EnvironmentStateChangeManager : IEnvironmentStateChangeManager
    {
        private const string LogBaseName = "environment_state_change_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentStateChangeManager"/> class.
        /// </summary>
        /// <param name="environmentStateChangeRepository">the environment state change repository.</param>
        /// <param name="planManager">the plan manager.</param>
        public EnvironmentStateChangeManager(
            IEnvironmentStateChangeRepository environmentStateChangeRepository,
            IPlanManager planManager)
        {
            EnvironmentStateChangeRepository = environmentStateChangeRepository;
            PlanManager = planManager;
        }

        private IEnvironmentStateChangeRepository EnvironmentStateChangeRepository { get; }

        private IPlanManager PlanManager { get; }

        /// <inheritdoc />
        public Task CreateAsync(EnvironmentStateChange change, IDiagnosticsLogger logger)
        {
            // retries up to three times, spaced out by 500ms
            return logger.RetryOperationScopeAsync(
                $"{LogBaseName}_create",
                async (childLogger) =>
                {
                    await EnvironmentStateChangeRepository.CreateOrUpdateAsync(change, logger.NewChildLogger());
                });
        }

        /// <inheritdoc />
        public async Task CreateAsync(VsoPlanInfo plan, EnvironmentBillingInfo environment, CloudEnvironmentState oldState, CloudEnvironmentState newState, IDiagnosticsLogger logger)
        {
            var fullPlan = await PlanManager.GetAsync(plan, logger);

            // Create the new billing state change
            var environmentStateChange = new EnvironmentStateChange()
            {
                OldValue = (oldState == default ? CloudEnvironmentState.Created : oldState).ToString(),
                NewValue = newState.ToString(),
                Environment = environment,
                Plan = plan,
                PlanId = fullPlan.Id,
                Time = DateTime.UtcNow,
            };

            await CreateAsync(environmentStateChange, logger.NewChildLogger());
        }

        /// <inheritdoc />
        public Task<IEnumerable<EnvironmentStateChange>> GetAllRecentEnvironmentEvents(string planId, DateTime startTime, DateTime endTime, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(planId, nameof(planId));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_all_recent_environment_events",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue(BillingLoggingConstants.PlanId, planId);
                    childLogger.FluentAddValue("StartTime", startTime);
                    childLogger.FluentAddValue("EndTime", endTime);

                    return await EnvironmentStateChangeRepository.GetAllEnvironmentEventsAsync(planId, startTime, endTime, childLogger.NewChildLogger());
                });
        }

        /// <inheritdoc/>
        public Task<IEnumerable<EnvironmentStateChange>> GetAllStateChanges(string planId, DateTime endTime, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(planId, nameof(planId));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_all_state_changes",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue(BillingLoggingConstants.PlanId, planId);
                    childLogger.FluentAddValue("EndTime", endTime);

                    return await EnvironmentStateChangeRepository.GetAllEnvironmentEventsAsync(planId, endTime, childLogger.NewChildLogger());
                });
        }
    }
}
