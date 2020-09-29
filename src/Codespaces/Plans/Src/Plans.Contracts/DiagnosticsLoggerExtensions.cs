// <copyright file="DiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// Logging extensions for <see cref="BillingEvent"/>.
    /// </summary>
    public static class DiagnosticsLoggerExtensions
    {
        private const string LogValueSubscriptionId = "SubscriptionId";
        private const string LogValueResourceGroupName = "ResourceGroup";
        private const string LogValueResourceId = "ResourceId";
        private const string LogValuePartner = "Partner";
        private const string LogValuePlanName = "Plan";
        private const string LogValuePlanTenantName = "PlanTenant";

        /// <summary>
        /// Add logging fields for a <see cref="VsoPlan"/> instance.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="plan">The plan, or null.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddVsoPlan(this IDiagnosticsLogger logger, VsoPlan plan)
        {
            Requires.NotNull(logger, nameof(logger));

            if (plan != null)
            {
                logger.AddPartner(plan.Partner)
                    .AddPlanTenant(plan.Tenant)
                    .AddVsoPlanInfo(plan.Plan);
            }

            return logger;
        }

        /// <summary>
        /// Add logging fields for a <see cref="VsoPlanInfo"/> instance.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="plan">The plan, or null.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddVsoPlanInfo(this IDiagnosticsLogger logger, VsoPlanInfo plan)
        {
            Requires.NotNull(logger, nameof(logger));

            if (plan != null)
            {
                logger
                    .AddSubscriptionId(plan.Subscription)
                    .AddResourceGroupName(plan.ResourceGroup)
                    .AddResourceId(plan.ResourceId)
                    .AddPlanName(plan.Name);
            }

            return logger;
        }

        /// <summary>
        /// Add logging fields for a <see cref="VsoPlanInfo"/> instance.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="planId">The plan id, or null.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddVsoPlanInfo(this IDiagnosticsLogger logger, string planId)
        {
            Requires.NotNull(logger, nameof(logger));

            if (planId != null && VsoPlanInfo.TryParse(planId, out var plan))
            {
                logger.AddVsoPlanInfo(plan);
            }

            return logger;
        }

        /// <summary>
        /// Add the partner to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="partner">The partner.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddPartner(this IDiagnosticsLogger logger, Partner? partner)
            => logger.FluentAddBaseValue(LogValuePartner, partner?.ToString());

        /// <summary>
        /// Add the subscription id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddSubscriptionId(this IDiagnosticsLogger logger, string subscriptionId)
            => logger.FluentAddBaseValue(LogValueSubscriptionId, subscriptionId);

        /// <summary>
        /// Add the plan resource group name to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="resourceGroupName">The resource group name.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddResourceGroupName(this IDiagnosticsLogger logger, string resourceGroupName)
            => logger.FluentAddBaseValue(LogValueResourceGroupName, resourceGroupName);

        /// <summary>
        /// Add the plan resource id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="resourceId">The resource id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddResourceId(this IDiagnosticsLogger logger, string resourceId)
            => logger.FluentAddBaseValue(LogValueResourceId, resourceId);

        /// <summary>
        /// Add the plan name to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="planName">The plan name.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddPlanName(this IDiagnosticsLogger logger, string planName)
            => logger.FluentAddBaseValue(LogValuePlanName, planName);

        /// <summary>
        /// Add the plan tenant ID value to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="planTenant">The plan tenant ID.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddPlanTenant(this IDiagnosticsLogger logger, string planTenant)
            => logger.FluentAddBaseValue(LogValuePlanTenantName, planTenant);
    }
}
