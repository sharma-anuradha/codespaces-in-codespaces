// <copyright file="DiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity
{
    /// <summary>
    /// Diagnostic logger extensions
    /// </summary>
    public static class DiagnosticsLoggerExtensions
    {
        /// <summary>
        /// Add the subscription id to the logger
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="subscriptionId">The subscription id</param>
        /// <returns>The logger with subscription id added</returns>
        public static IDiagnosticsLogger AddSubscriptionId(this IDiagnosticsLogger logger, string subscriptionId)
        {
            return logger
                .FluentAddBaseValue(LogNames.SubscriptionId, subscriptionId)
                .FluentAddBaseValue(LogNames.Subscription, subscriptionId);
        }

        /// <summary>
        /// Add the subscription name to the logger
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="subscriptionName">The subscription name</param>
        /// <returns>The logger with subscription name added</returns>
        public static IDiagnosticsLogger AddSubscriptionName(this IDiagnosticsLogger logger, string subscriptionName)
        {
            return logger
                .FluentAddBaseValue(LogNames.SubscriptionName, subscriptionName);
        }

        /// <summary>
        /// Add the service type to the logger
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="serviceType">The service type</param>
        /// <returns>The logger with values added</returns>
        public static IDiagnosticsLogger AddServiceType(this IDiagnosticsLogger logger, string serviceType)
        {
            return logger
                .FluentAddBaseValue(LogNames.ServiceType, serviceType)
                .FluentAddBaseValue(LogNames.SubscriptionServiceType, serviceType);
        }

        /// <summary>
        /// Add the service type to the logger
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="serviceType">The service type</param>
        /// <returns>The logger with values added</returns>
        public static IDiagnosticsLogger AddServiceType(this IDiagnosticsLogger logger, ServiceType? serviceType)
        {
            return logger.AddServiceType(serviceType?.ToString());
        }

        /// <summary>
        /// Add the capacity metric values to the logger
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="quota">The quota type</param>
        /// <param name="limit">The quota limit</param>
        /// <param name="currentValue">The quota current usage value</param>
        /// <param name="usedPercent">The quota current usage percentage</param>
        /// <returns>The logger with values added</returns>
        public static IDiagnosticsLogger AddSubscriptionCapacityValues(this IDiagnosticsLogger logger, string quota, long limit, long currentValue, double usedPercent)
        {
            return logger
                .FluentAddBaseValue(LogNames.Quota, quota)
                .FluentAddBaseValue(LogNames.Limit, limit)
                .FluentAddBaseValue(LogNames.CurrentValue, currentValue)
                .FluentAddBaseValue(LogNames.UsedPercent, usedPercent);
        }

        /// <summary>
        /// Add the aggregate capacity metric values to the logger
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="limitForMixedSubs">The quota limit for mixed subscriptions</param>
        /// <param name="currentForMixedSubs">The quota current usage for mixed subscriptions</param>
        /// <param name="limitForServiceTypeSpecificSubs">The quota limit for single type subscriptions</param>
        /// <param name="currentForServiceTypeSpecificSubs">The quota current usage for single type subscriptions</param>
        /// <returns>The logger with values added</returns>
        public static IDiagnosticsLogger AddAggregateCapacityValues(
            this IDiagnosticsLogger logger,
            long limitForMixedSubs,
            long currentForMixedSubs,
            long limitForServiceTypeSpecificSubs,
            long currentForServiceTypeSpecificSubs)
        {
            return logger
                .FluentAddBaseValue(LogNames.LimitForMixedSubs, limitForMixedSubs)
                .FluentAddBaseValue(LogNames.CurrentForMixedSubs, currentForMixedSubs)
                .FluentAddBaseValue(LogNames.LimitForServiceTypeSpecificSubs, limitForServiceTypeSpecificSubs)
                .FluentAddBaseValue(LogNames.CurrentForServiceTypeSpecificSubs, currentForServiceTypeSpecificSubs);
        }

        /// <summary>
        /// Add the subscription enablement flag to the logger
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="enabled">The enabled flag</param>
        /// <returns>The logger with values added</returns>
        public static IDiagnosticsLogger AddSubscriptionIsEnabled(this IDiagnosticsLogger logger, bool? enabled)
        {
            return logger
                .FluentAddBaseValue(LogNames.Enabled, enabled);
        }

        /// <summary>
        /// Add the <see cref="AzureLocation"/> to the logger
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="location">The location</param>
        /// <returns>The logger with values added</returns>
        public static IDiagnosticsLogger AddAzureLocation(this IDiagnosticsLogger logger, AzureLocation? location)
        {
            return logger
                .FluentAddBaseValue(LogNames.Location, location);
        }

        /// <summary>
        /// Add the subscription type to the logger
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="subscriptionType">The subscription type (e.g. Data or Infrastructure)</param>
        /// <returns>The logger with values added</returns>
        public static IDiagnosticsLogger AddSubscriptionType(this IDiagnosticsLogger logger, string subscriptionType)
        {
            return logger
                .FluentAddBaseValue(LogNames.SubscriptionType, subscriptionType);
        }

        /// <summary>
        /// Constants for telemetry dimension and metric names used for capacity monitoring
        /// </summary>
        /// <remarks>
        /// Note: some names are camelCase for backwards compatibility. When adding a new name, use PascalCase.
        /// </remarks>
        private class LogNames
        {
            /// <summary>
            /// The subscription id
            /// </summary>
            public const string SubscriptionId = "subscriptionId";

            /// <summary>
            /// The subscription id
            /// </summary>
            /// <remarks>
            /// For backwards compatibility - use <see cref="SubscriptionId"/>.
            /// </remarks>
            public const string Subscription = "subscription";

            /// <summary>
            /// The subscription name
            /// </summary>
            public const string SubscriptionName = "subscriptionName";

            /// <summary>
            /// The <see cref="ServiceType"/>
            /// </summary>
            public const string ServiceType = "serviceType";

            /// <summary>
            /// The <see cref="ServiceType"/>
            /// </summary>
            /// <remarks>
            /// For backwards compatibility - use <see cref="ServiceType"/>.
            /// </remarks>
            public const string SubscriptionServiceType = "subscriptionServiceType";

            /// <summary>
            /// The quota type
            /// </summary>
            public const string Quota = "quota";

            /// <summary>
            /// The limit for the quota
            /// </summary>
            public const string Limit = "limit";

            /// <summary>
            /// The current usage of the quota
            /// </summary>
            public const string CurrentValue = "currentValue";

            /// <summary>
            /// The current usage percentage of the quota (equal to <see cref="CurrentValue"/>/<see cref="Limit"/>)
            /// </summary>
            public const string UsedPercent = "usedPercent";

            /// <summary>
            /// The <see cref="AzureLocation"/>
            /// </summary>
            public const string Location = "location";

            /// <summary>
            /// The subscription enabled status
            /// </summary>
            public const string Enabled = "enabled";

            /// <summary>
            /// The aggregate limit for mixed type subscriptions
            /// </summary>
            public const string LimitForMixedSubs = "limitForMixedSubs";

            /// <summary>
            /// The aggregate current value for mixed type subscriptions
            /// </summary>
            public const string CurrentForMixedSubs = "currentForMixedSubs";

            /// <summary>
            /// The aggregate limit for single type subscriptions
            /// </summary>
            public const string LimitForServiceTypeSpecificSubs = "limitForServiceTypeSpecificSubs";

            /// <summary>
            /// The aggregate current value for single type subscriptions
            /// </summary>
            public const string CurrentForServiceTypeSpecificSubs = "currentForServiceTypeSpecificSubs";

            /// <summary>
            /// The subscription type (e.g. Data or Infrastructure)
            /// </summary>
            public const string SubscriptionType = nameof(SubscriptionType);
        }
    }
}
