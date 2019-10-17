// <copyright file="DiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Logging extensions for <see cref="BillingEvent"/>.
    /// </summary>
    public static class DiagnosticsLoggerExtensions
    {
        private const string LogValueSubscriptionId = "SubscriptionId";
        private const string LogValueResourceGroupName = "ResourceGroup";
        private const string LogValuePlanName = "Plan";

        /// <summary>
        /// Add logging fields for a <see cref="VsoPlanInfo"/> instance.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="plan">The plan, or null.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddVsoPlan(this IDiagnosticsLogger logger, VsoPlanInfo plan)
        {
            Requires.NotNull(logger, nameof(logger));

            if (plan != null)
            {
                logger
                    .AddSubscriptionId(plan.Subscription)
                    .AddResourceGroupName(plan.ResourceGroup)
                    .AddPlanName(plan.Name);
            }

            return logger;
        }

        /// <summary>
        /// Add the subscription id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddSubscriptionId(this IDiagnosticsLogger logger, string subscriptionId)
            => logger.FluentAddValue(LogValueSubscriptionId, subscriptionId);

        /// <summary>
        /// Add the environment owner id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="resourceGroupName">The resource group name.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddResourceGroupName(this IDiagnosticsLogger logger, string resourceGroupName)
            => logger.FluentAddValue(LogValueResourceGroupName, resourceGroupName);

        /// <summary>
        /// Add the environment connection session id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="planName">The plan name.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddPlanName(this IDiagnosticsLogger logger, string planName)
            => logger.FluentAddValue(LogValuePlanName, planName);
    }
}
