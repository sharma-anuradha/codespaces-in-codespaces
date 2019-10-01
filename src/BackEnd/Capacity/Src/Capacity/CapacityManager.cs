// <copyright file="CapacityManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity
{
    /// <summary>
    /// The capacity manager.
    /// </summary>
    public class CapacityManager : ICapacityManager
    {
        private const double UsageThresholdPercent = 0.80;
        private const int MaxResourceGroupsPerSubscription = 980;

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityManager"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        /// <param name="azureSubscriptionCapacity">The azure subscription capacity data.</param>
        /// <param name="controlPlaneInfo">The control-plane resource accessor.</param>
        /// <param name="resourceNameBuilder">resource name builder.</param>
        /// <param name="capacitySettings">Capacity settings.</param>
        public CapacityManager(
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            IAzureSubscriptionCapacityProvider azureSubscriptionCapacity,
            IControlPlaneInfo controlPlaneInfo,
            IResourceNameBuilder resourceNameBuilder,
            CapacitySettings capacitySettings)
        {
            AzureSubscriptionCatalog = Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            AzureSubscriptionCapacity = Requires.NotNull(azureSubscriptionCapacity, nameof(azureSubscriptionCapacity));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            CapacitySettings = Requires.NotNull(capacitySettings, nameof(capacitySettings));
        }

        private Random Rnd { get; } = new Random();

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private IAzureSubscriptionCapacityProvider AzureSubscriptionCapacity { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private CapacitySettings CapacitySettings { get; }

        /// <inheritdoc/>
        public async Task<IAzureResourceLocation> SelectAzureResourceLocation(IEnumerable<AzureResourceCriterion> criteria, AzureLocation location, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(criteria, nameof(criteria));
            Requires.NotNull(logger, nameof(logger));

            var enabledSubscriptionForLocation =
                from subscription in AzureSubscriptionCatalog.AzureSubscriptions
                where subscription.Enabled
                where subscription.Locations.Contains(location)
                select subscription;

            if (!enabledSubscriptionForLocation.Any())
            {
                logger
                    .FluentAddValue(nameof(location), location)
                    .LogError("capacity_manager_subscriptions_not_available_error");
                throw new LocationNotAvailableException(location);
            }

            var subscriptionsWithAllCriteria = new Dictionary<IAzureSubscription, AzureResourceUsage>();

            foreach (var subscription in enabledSubscriptionForLocation)
            {
                try
                {
                    var allRequiredCriteria = true;
                    var primaryUsage = default(AzureResourceUsage);

                    // Older versions of the resource broker might have serialized create-inputs
                    // that don't have all of the necessary criteria ingredients. Filter those out.
                    foreach (var criterion in criteria
                        .Where(c => !string.IsNullOrEmpty(c.Quota))
                        .Where(c => c.Required > 0))
                    {
                        // Get all usage data for this subscription/location
                        var azureResourceUsages = await AzureSubscriptionCapacity.GetAzureResourceUsageAsync(
                            subscription,
                            location,
                            criterion.ServiceType,
                            logger);

                        // Select the usage that matches the current criterion
                        var match = (
                            from usage in azureResourceUsages
                            where usage.Quota == criterion.Quota
                            let available = usage.Limit - usage.CurrentValue
                            where available >= criterion.Required
                            select usage)
                            .FirstOrDefault();

                        // If no match, bail out. Note that all crtieria must match.
                        if (match == null)
                        {
                            allRequiredCriteria = false;
                            break;
                        }

                        // Capture the first usage for later use.
                        primaryUsage = primaryUsage ?? match;
                    }

                    if (allRequiredCriteria)
                    {
                        subscriptionsWithAllCriteria[subscription] = primaryUsage;
                    }
                }
                catch (CapacityNotFoundException ex)
                {
                    logger.LogException("capacity_manager_capcity_not_found", ex);

                    // Ignore this subscription if we couldn't read the capacity, but keep trying...
                    continue;
                }
            }

            // Couldn't find one that meets all criteria.
            if (!subscriptionsWithAllCriteria.Any())
            {
                logger
                    .FluentAddValue(nameof(location), location)
                    .FluentAddValue("subscriptions", string.Join(",", enabledSubscriptionForLocation.Select(s => s.DisplayName)))
                    .FluentAddValue("criteria", string.Join(",", criteria.Select(c => $"{c.ServiceType}/{c.Quota}:{c.Required}")))
                    .LogError("capacity_manager_capacity_not_available_error");

                throw new CapacityNotAvailableException(location, criteria.Select(c => $"{c.ServiceType}/{c.Quota}"));
            }

            /*
            // Now we have a set of subscriptions that match the minimal criteria.
            // We want to fill all subscriptions to 80%, starting with the ones with the highest limit.
            // We then want to fill all subscriptions to capacity, starting with the ones with the highest available.
            // In case of a tie-breaker, sort by the subscdription id
            */

            var subscriptionsUnderUsageThresholdByHighestLimit =
                from item in subscriptionsWithAllCriteria
                let subscription = item.Key
                let usage = item.Value
                let limit = usage.Limit
                let current = usage.CurrentValue
                let available = limit - current
                let percentUsed = limit > 0 ? (double)current / (double)limit : 0.0
                where percentUsed < UsageThresholdPercent
                orderby limit descending, subscription.SubscriptionId
                select subscription;

            var subscriptionsByHighestAbsoluteAvailable =
                from item in subscriptionsWithAllCriteria
                let subscription = item.Key
                let usage = item.Value
                let limit = usage.Limit
                let current = usage.CurrentValue
                let available = limit - current
                orderby available descending, subscription.SubscriptionId
                select subscription;

            // Select one random subscription of the selected ones
            var theSubscription =
                subscriptionsUnderUsageThresholdByHighestLimit.FirstOrDefault() ??
                subscriptionsByHighestAbsoluteAvailable.First();
            var stampResourceGroupName = ControlPlaneInfo.Stamp.StampResourceGroupName;
            var resourceGroupName = GetBaseResourceGroupName();

            // We'll used resource group names that match the stamp resource group, so that it will be clear which
            // stamp has allocated the data-plane resource groups. For example, production in East US would yield the name
            // vsclk-online-prod-rel-use-###
            if (CapacitySettings.SpreadResourcesInGroups)
            {
                var resourceGroupNumber = GetRandomResourceGroupNumber();
                resourceGroupName = $"{resourceGroupName}-{resourceGroupNumber:000}";
            }

            logger
                .FluentAddValue("subscription", theSubscription.DisplayName)
                .FluentAddValue("subscriptionId", theSubscription.SubscriptionId)
                .FluentAddValue("resourceGroup", resourceGroupName)
                .FluentAddValue(nameof(location), location)
                .FluentAddValue("candidateSubscriptions", string.Join(",", enabledSubscriptionForLocation.Select(s => s.DisplayName)))
                .FluentAddValue("criteria", string.Join(",", criteria.Select(c => $"{c.ServiceType}/{c.Quota}:{c.Required}")))
                .LogInfo("capacity_manager_select_azure_resource_location");

            return new AzureResourceLocation(theSubscription, resourceGroupName, location);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<IAzureResourceGroup>> SelectAllAzureResourceGroups(IAzureClientFactory azureClientFactory)
        {
            var results = new List<IAzureResourceGroup>();
            var resourceGroupNamePrefix = GetBaseResourceGroupName();
            foreach (var subscription in AzureSubscriptionCatalog.AzureSubscriptions)
            {
                var azure = await azureClientFactory.GetAzureClientAsync(Guid.Parse(subscription.SubscriptionId));
                var resourceGroups = await azure.ResourceGroups.ListAsync();
                foreach (var resourceGroup in resourceGroups)
                {
                    if (resourceGroup.Name.StartsWith(resourceGroupNamePrefix))
                    {
                        results.Add(new AzureResourceGroup(subscription, resourceGroup.Name));
                    }
                }
            }

            return results;
        }

        private string GetBaseResourceGroupName()
        {
            var stampResourceGroupName = ControlPlaneInfo.Stamp.StampResourceGroupName;
            var resourceGroupName = ResourceNameBuilder.GetResourceGroupName(stampResourceGroupName);
            return resourceGroupName;
        }

        private int GetRandomResourceGroupNumber()
        {
            // Handle unset or unreasonable capacity settings values.
            var min = Math.Max(1, CapacitySettings.Min);    // Min is at least 1
            var max = Math.Max(100, CapacitySettings.Max);  // Max is at least 100
            max = Math.Min(MaxResourceGroupsPerSubscription, max);
            return Rnd.Next(min, max);
        }
    }
}
