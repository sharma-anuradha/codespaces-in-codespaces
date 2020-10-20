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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity
{
    /// <summary>
    /// The capacity manager.
    /// </summary>
    public class CapacityManager : ICapacityManager
    {
        private const int MaxResourceGroupsPerSubscription = 980;
        private const string ConfigSettingComponentName = "capacitymanager";

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityManager"/> class.
        /// </summary>
        /// <param name="azureClientFactory">The factory for getting clients to query Azure.</param>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        /// <param name="azureSubscriptionCapacity">The azure subscription capacity data.</param>
        /// <param name="controlPlaneInfo">The control-plane resource accessor.</param>
        /// <param name="resourceNameBuilder">resource name builder.</param>
        /// <param name="configurationReader">A configuration reader instance.</param>
        /// <param name="capacitySettings">Capacity settings.</param>
        public CapacityManager(
            IAzureClientFactory azureClientFactory,
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            IAzureSubscriptionCapacityProvider azureSubscriptionCapacity,
            IControlPlaneInfo controlPlaneInfo,
            IResourceNameBuilder resourceNameBuilder,
            IConfigurationReader configurationReader,
            CapacitySettings capacitySettings)
        {
            AzureClientFactory = Requires.NotNull(azureClientFactory, nameof(azureClientFactory));
            AzureSubscriptionCatalog = Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            AzureSubscriptionCapacity = Requires.NotNull(azureSubscriptionCapacity, nameof(azureSubscriptionCapacity));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            ConfigurationReader = Requires.NotNull(configurationReader, nameof(configurationReader));
            CapacitySettings = Requires.NotNull(capacitySettings, nameof(capacitySettings));
        }

        private Random Rnd { get; } = new Random();

        private IAzureClientFactory AzureClientFactory { get; }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private IAzureSubscriptionCapacityProvider AzureSubscriptionCapacity { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private IConfigurationReader ConfigurationReader { get; }

        private CapacitySettings CapacitySettings { get; }

        /// <inheritdoc/>
        public async Task<IAzureResourceLocation> SelectAzureResourceLocation(IEnumerable<AzureResourceCriterion> criteria, AzureLocation location, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(criteria, nameof(criteria));
            Requires.NotNull(logger, nameof(logger));

            var enabledSubscriptionForLocation = await GetEnabledSubscriptionsForLocation(location, logger);

            if (!enabledSubscriptionForLocation.Any())
            {
                logger
                    .FluentAddValue(nameof(location), location)
                    .LogError("capacity_manager_subscriptions_not_available_error");
                throw new LocationNotAvailableException(location);
            }

            // Older versions of the resource broker might have serialized create-inputs
            // that don't have all of the necessary criteria ingredients. Filter those out.
            var validCriteria = criteria
                .Where(c => !string.IsNullOrEmpty(c.Quota))
                .Where(c => c.Required > 0)
                .ToArray();

            // The subscription must either support all ServiceTypes, or be able to handle all required ServiceTypes to be considered.
            var subscriptionsWithAllRequiredServiceTypes = enabledSubscriptionForLocation
                .Where(subscription =>
                    subscription.ServiceType == null ||
                    validCriteria.All(criterion => criterion.ServiceType == subscription.ServiceType));

            if (!subscriptionsWithAllRequiredServiceTypes.Any())
            {
                logger
                    .FluentAddValue(nameof(location), location)
                    .AddResourceCriteria(criteria)
                    .FluentAddValue("enabledSubscriptions", string.Join(",", enabledSubscriptionForLocation.Select(s => s.DisplayName)))
                    .LogError("capacity_manager_subscriptions_match_service_types_error");
                throw new LocationNotAvailableException(location);
            }

            var subscriptionsWithAllCriteria = new Dictionary<IAzureSubscription, AzureResourceUsage>();

            foreach (var subscription in subscriptionsWithAllRequiredServiceTypes)
            {
                try
                {
                    var allRequiredCriteria = true;
                    var primaryUsage = default(AzureResourceUsage);

                    foreach (var criterion in validCriteria)
                    {
                        // Get all usage data for this subscription/location
                        var azureResourceUsage = await AzureSubscriptionCapacity.LoadAzureResourceUsageAsync(
                            subscription,
                            location,
                            criterion.ServiceType,
                            criterion.Quota,
                            logger);

                        // If no match, bail out. Note that all criteria must match.
                        if (azureResourceUsage == null || (azureResourceUsage.Limit - azureResourceUsage.CurrentValue) < criterion.Required)
                        {
                            allRequiredCriteria = false;
                            break;
                        }

                        // Capture the first usage for later use.
                        primaryUsage ??= azureResourceUsage;
                    }

                    if (allRequiredCriteria)
                    {
                        subscriptionsWithAllCriteria[subscription] = primaryUsage;
                    }
                }
                catch (CapacityNotFoundException ex)
                {
                    logger.LogException("capacity_manager_capacity_not_found", ex);

                    // Ignore this subscription if we couldn't read the capacity, but keep trying...
                    continue;
                }
            }

            var matchedServiceSpecificSubscriptions = subscriptionsWithAllCriteria.Any(s => s.Key.ServiceType != null);
            if (matchedServiceSpecificSubscriptions)
            {
                // If there are matched service type specific subscriptions only use those.
                subscriptionsWithAllCriteria = subscriptionsWithAllCriteria
                                                .Where(s => s.Key.ServiceType != null)
                                                .ToDictionary(s => s.Key, s => s.Value);
            }

            // Couldn't find one that meets all criteria.
            if (!subscriptionsWithAllCriteria.Any())
            {
                logger
                    .FluentAddValue(nameof(location), location)
                    .FluentAddValue("subscriptions", string.Join(",", subscriptionsWithAllRequiredServiceTypes.Select(s => s.DisplayName)))
                    .AddResourceCriteria(criteria)
                    .LogError("capacity_manager_capacity_not_available_error");

                throw new CapacityNotAvailableException(location, criteria.Select(c => $"{c.ServiceType}/{c.Quota}"));
            }

            var theSubscription = SelectSubscription(subscriptionsWithAllCriteria);
            var resourceGroupName = GetBaseResourceGroupName();

            // We'll used resource group names that match the stamp resource group, so that it will be clear which
            // stamp has allocated the data-plane resource groups. For example, production in East US would yield the name
            // vsclk-online-prod-rel-use-###
            if (CapacitySettings.SpreadResourcesInGroups)
            {
                var resourceGroupNumber = GetRandomResourceGroupNumber(theSubscription);
                resourceGroupName = $"{resourceGroupName}-{resourceGroupNumber:000}";
            }

            logger
                .FluentAddValue("subscription", theSubscription.DisplayName)
                .FluentAddValue("subscriptionId", theSubscription.SubscriptionId)
                .FluentAddValue("resourceGroup", resourceGroupName)
                .FluentAddValue(nameof(location), location)
                .FluentAddValue("candidateSubscriptions", string.Join(",", subscriptionsWithAllRequiredServiceTypes.Select(s => s.DisplayName)))
                .AddResourceCriteria(criteria)
                .LogInfo("capacity_manager_select_azure_resource_location");

            return new AzureResourceLocation(theSubscription, resourceGroupName, location);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<IAzureResourceGroup>> GetAllDataPlaneResourceGroups(IDiagnosticsLogger logger)
        {
            var results = new List<IAzureResourceGroup>();
            var resourceGroupNamePrefix = GetBaseResourceGroupName();
            foreach (var subscription in AzureSubscriptionCatalog.AzureSubscriptions)
            {
                var subscriptionIsEnabled = await CheckSubscriptionIsEnabled(subscription, logger);
                if (!subscriptionIsEnabled)
                {
                    continue;
                }

                var azure = await AzureClientFactory.GetAzureClientAsync(Guid.Parse(subscription.SubscriptionId));
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

        private static IEnumerable<KeyValuePair<IAzureSubscription, AzureResourceUsage>> SubscriptionsByAvailableDescending(
            Dictionary<IAzureSubscription, AzureResourceUsage> subscriptionUsageMap)
        {
            return
                from item in subscriptionUsageMap
                let subscription = item.Key
                let usage = item.Value
                let limit = usage.Limit
                let current = usage.CurrentValue
                let available = limit - current
                orderby available descending
                select item;
        }

        private static IAzureSubscription SelectSubscription(Dictionary<IAzureSubscription, AzureResourceUsage> subscriptionsWithAllCriteria)
        {
            /*
            // How to select a subscription that is most likely to succeed, knowing that
            // the usage data may be stale, and a subscription might get overloaded until
            // the next capacity update?
            //
            // Get the set of subscriptions that represent the top 50th percentile and pick
            // randomly from that set.
            */

            var subscriptionCount = subscriptionsWithAllCriteria.Count;
            var totalCapacity = subscriptionsWithAllCriteria.Values.Sum(usage => usage.Limit - usage.CurrentValue);
            var subscriptionsByAvailableDescending = SubscriptionsByAvailableDescending(subscriptionsWithAllCriteria).ToArray();
            var topHalf = new List<IAzureSubscription>();
            var sumCapacity = 0L;
            const int minimumCount = 3;
            const double percentile = 0.5;
            foreach (var item in subscriptionsByAvailableDescending)
            {
                topHalf.Add(item.Key);
                if (subscriptionCount > minimumCount)
                {
                    sumCapacity += item.Value.Limit - item.Value.CurrentValue;
                    if (((double)sumCapacity / (double)totalCapacity) > percentile)
                    {
                        break;
                    }
                }
            }

            var theSubscription = topHalf.RandomOrDefault();
            return theSubscription;
        }

        private async Task<bool> CheckSubscriptionIsEnabled(IAzureSubscription subscription, IDiagnosticsLogger logger)
        {
            const string loggingName = "capacity_manager_check_subscription_enabled";

            return await logger.OperationScopeAsync(
                loggingName,
                async (childLogger) =>
                {
                    childLogger.FluentAddValue("SubscriptionId", subscription.SubscriptionId)
                        .FluentAddValue("SubscriptionName", subscription.DisplayName);

                    var settingName = $"subscription-enabled-{subscription.SubscriptionId}";
                    var subscriptionIsEnabled = await ConfigurationReader.ReadSettingAsync(ConfigSettingComponentName, settingName, logger, subscription.Enabled);

                    childLogger.FluentAddValue("SubscriptionIsEnabledInAppSettings", subscription.Enabled)
                        .FluentAddValue("SubscriptionIsEnabled", subscriptionIsEnabled);

                    return subscriptionIsEnabled;
                },
                swallowException: true);
        }

        /// <summary>
        /// Gets a list of enabled subscriptions for a given location.
        /// </summary>
        private async Task<IEnumerable<IAzureSubscription>> GetEnabledSubscriptionsForLocation(AzureLocation location, IDiagnosticsLogger logger)
        {
            Requires.NotNull(logger, nameof(logger));

            const string loggingName = "capacity_manager_get_enabled_subscriptions_for_location";

            return await logger.OperationScopeAsync<IEnumerable<IAzureSubscription>>(
                loggingName,
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("AzureLocation", location.ToString());

                    var enabledSubscriptions = new List<IAzureSubscription>();

                    var subscriptions = from subscription in AzureSubscriptionCatalog.AzureSubscriptions
                        where subscription.Locations.Contains(location)
                        select subscription;

                    foreach (var subscription in subscriptions)
                    {
                        var subscriptionIsEnabled = await CheckSubscriptionIsEnabled(subscription, logger);
                        if (subscriptionIsEnabled)
                        {
                            enabledSubscriptions.Add(subscription);
                        }
                    }

                    var disabledSubscriptions = subscriptions.Except(enabledSubscriptions);

                    childLogger.FluentAddValue("EnabledSubscriptions", string.Join(",", enabledSubscriptions.Select(s => s.SubscriptionId)))
                        .FluentAddValue("DisabledSubscriptions", string.Join(",", disabledSubscriptions.Select(s => s.SubscriptionId)));

                    return enabledSubscriptions;
                },
                swallowException: false);
        }

        private string GetBaseResourceGroupName()
        {
            var stampResourceGroupName = ControlPlaneInfo.Stamp.StampResourceGroupName;
            var resourceGroupName = ResourceNameBuilder.GetResourceGroupName(stampResourceGroupName);
            return resourceGroupName;
        }

        private int GetRandomResourceGroupNumber(IAzureSubscription azureSubscription)
        {
            // Handle unset or unreasonable capacity settings values.
            var min = Math.Max(1, CapacitySettings.Min);    // Min is at least 1
            var max = Math.Min(azureSubscription.MaxResourceGroupCount, MaxResourceGroupsPerSubscription);  // Max is configure in subscription (default 100) and cannot exceed 980
            return Rnd.Next(min, max);
        }
    }
}
