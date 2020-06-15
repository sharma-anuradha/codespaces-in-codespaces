// <copyright file="AzureResourceIdExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// Validate Azure resource id.
    /// </summary>
    public static class AzureResourceIdExtensions
    {
        private const string NetworkProviderName = "Microsoft.Network";
        private const string VnetResourceType = "virtualNetworks";
        private const string SubnetResourceType = "subnets";

        /// <summary>
        /// Validate subnet resource id format.
        /// </summary>
        /// <param name="resourceId">resource id.</param>
        /// <param name="logger">logger instance.</param>
        /// <returns>result.</returns>
        public static bool IsValidSubnetResourceId(this string resourceId, IDiagnosticsLogger logger)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                logger.LogErrorWithDetail("resourceid_validate_error", $"null or empty {nameof(resourceId)} value not supported.");
                return false;
            }

            var azureResourceId = (ResourceId)default;
            try
            {
                azureResourceId = ResourceId.FromString(resourceId);
            }
            catch (ArgumentException ex)
            {
                logger.LogException("resourceid_validate_error", ex);
                return false;
            }

            if (azureResourceId == default)
            {
                return false;
            }

            if (azureResourceId.ProviderNamespace != NetworkProviderName)
            {
                return false;
            }

            if (azureResourceId.ResourceType != SubnetResourceType)
            {
                return false;
            }

            if (azureResourceId.Parent.ResourceType != VnetResourceType)
            {
                return false;
            }

            if (!azureResourceId.SubscriptionId.IsValidSubscriptionId())
            {
                return false;
            }

            if (!azureResourceId.ResourceGroupName.IsValidResourceGroupName())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate subscription id.
        /// </summary>
        /// <param name="subscriptionId">subscriptionId.</param>
        /// <returns>result.</returns>
        public static bool IsValidSubscriptionId(this string subscriptionId)
        {
            return Guid.TryParseExact(subscriptionId, "D", out _);
        }

        /// <summary>
        /// Validate resource group name.
        /// </summary>
        /// <param name="resourceGroupName">resourceGroupName.</param>
        /// <returns>result.</returns>
        public static bool IsValidResourceGroupName(this string resourceGroupName)
        {
            // https://docs.microsoft.com/en-us/azure/architecture/best-practices/naming-conventions#naming-rules-and-restrictions
            return !string.IsNullOrWhiteSpace(resourceGroupName) && resourceGroupName.Length <= 90;
        }
    }
}
