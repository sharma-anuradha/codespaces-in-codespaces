// <copyright file="DiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Logging extensions for <see cref="BillingEvent"/>.
    /// </summary>
    public static class DiagnosticsLoggerExtensions
    {
        private const string LogValueSubscriptionId = "SubscriptionId";
        private const string LogValueResourceGroupName = "ResourceGroup";
        private const string LogValueAccountName = "Account";

        /// <summary>
        /// Add logging fields for a <see cref="VsoAccountInfo"/> instance.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="account">The account, or null.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddAccount(this IDiagnosticsLogger logger, VsoAccountInfo account)
        {
            Requires.NotNull(logger, nameof(logger));

            if (account != null)
            {
                logger
                    .AddSubscriptionId(account.Subscription)
                    .AddResourceGroupName(account.ResourceGroup)
                    .AddAccountName(account.Name);
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
        /// <param name="accountName">The account name.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddAccountName(this IDiagnosticsLogger logger, string accountName)
            => logger.FluentAddValue(LogValueAccountName, accountName);
    }
}
