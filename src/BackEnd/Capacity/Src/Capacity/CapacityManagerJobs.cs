// <copyright file="CapacityManagerJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
    public class CapacityManagerJobs : IAsyncBackgroundWarmup
    {
        // Update capacity constants
        private const string UpdateCapacityLeaseContainerName = "update-capacity-leases";
        private const string UpdateCapacityLeasePrefix = "update-capacity-lease";
        private const string UpdateCapacityLoopName = "update_capacity_loop";
        private const string UpdateCapacityOperationName = "update_capacity_operation";
        private const string UpdateCapacityTaskName = "update_capacity_job";

        // Monitor capacity constants
        private const string MonitorCapacityLeaseContainerName = "monitor-capacity-leases";
        private const string MonitorCapacityLeasePrefix = "monitor-capacity-lease";
        private const string MonitorCapacityLoopName = "monitor_capacity_loop";
        private const string MonitorCapacityOperationName = "monitor_capacity_operation";
        private const string MonitorCapacityTaskName = "monitor_capacity_job";
        private const string MonitorCapacityLogAlert = "monitor_capacity_alert";
        private const string MonitorCapacityLogInfo = "monitor_capacity_info";
        private const double MonitorCapacityQuotaThreshold = 0.80;
        private const double MonitorCapacityQuotaThresholdCritical = 0.95;

        // Update capacity parameters
        private static readonly TimeSpan UpdateCapacityInterval = Capacity.AzureSubscriptionCapacityProvider.UpdateInterval;
        private static readonly TimeSpan MonitorCapacityInterval = Capacity.AzureSubscriptionCapacityProvider.UpdateInterval;

        // Monitor capacity parameters
        private static readonly TimeSpan UpdateCapacityLeaseTimeSpan = UpdateCapacityInterval * 0.90;
        private static readonly TimeSpan MonitorCapacityLeaseTimeSpan = MonitorCapacityInterval * 0.90;

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityManagerJobs"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCapacityProvider">The azure subscription capacity.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="azureSubscriptionCatalog">The subscription catalog.</param>
        /// <param name="claimedDistributedLease">The distributed lease helper.</param>
        /// <param name="taskHelper">The task helper.</param>
        /// <param name="resourceNameBuilder">Resource name builder, for lease names.</param>
        /// <param name="developerPersonalStampSettings">Developer mode.</param>
        public CapacityManagerJobs(
            IAzureSubscriptionCapacityProvider azureSubscriptionCapacityProvider,
            IControlPlaneInfo controlPlaneInfo,
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IResourceNameBuilder resourceNameBuilder,
            DeveloperPersonalStampSettings developerPersonalStampSettings)
        {
            Requires.NotNull(azureSubscriptionCapacityProvider, nameof(azureSubscriptionCapacityProvider));
            Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
            Requires.NotNull(taskHelper, nameof(taskHelper));
            Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));

            AzureSubscriptionCapacityProvider = azureSubscriptionCapacityProvider;
            AzureSubscriptionCatalog = azureSubscriptionCatalog;
            ControlPlaneInfo = controlPlaneInfo;
            ClaimedDistributedLease = claimedDistributedLease;
            TaskHelper = taskHelper;
            ResourceNameBuilder = resourceNameBuilder;
            DeveloperStamp = developerPersonalStampSettings.DeveloperStamp;
        }

        private IAzureSubscriptionCapacityProvider AzureSubscriptionCapacityProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private ITaskHelper TaskHelper { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private bool DeveloperStamp { get; }

        /// <inheritdoc/>
        public async Task WarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;

            // Determine the work to be done in this control-plane.
            var dataPlaneLocations = new HashSet<AzureLocation>(ControlPlaneInfo.Stamp.DataPlaneLocations);
            var subscriptionLocations = AzureSubscriptionCatalog.AzureSubscriptions
                .SelectMany(subscription => subscription.Locations
                    .Where(location => dataPlaneLocations.Contains(location))
                    .Select(location => (subscription, location)))
                .ToArray();

            var computeSubscriptionLocations = subscriptionLocations.Where(s => s.subscription.ComputeQuotas.Any(quota => quota.Value > 0));
            var storageSubscriptionLocations = subscriptionLocations.Where(s => s.subscription.StorageQuotas.Any(quota => quota.Value > 0));
            var networkSubscriptionLocations = subscriptionLocations.Where(s => s.subscription.NetworkQuotas.Any(quota => quota.Value > 0));

            // Set up the loop intervals
            var updateCapacityInterval = UpdateCapacityInterval;
            var monitorCapacityInterval = MonitorCapacityInterval;
            if (DeveloperStamp)
            {
                // 10x for developer stamp
                updateCapacityInterval *= 0.1;
                monitorCapacityInterval *= 0.1;
            }

            // TODO: Candidate for RunBackgroundEnumerableAsync usage
            // Update the compute capacity
            foreach (var (subscription, location) in computeSubscriptionLocations)
            {
                await WaitASecond();
                UpdateAzureResourceUsageBackgroundLoop(subscription, location, ServiceType.Compute, updateCapacityInterval, logger);
            }

            // TODO: Candidate for RunBackgroundEnumerableAsync usage
            // Update the storage capacity
            foreach (var (subscription, location) in storageSubscriptionLocations)
            {
                await WaitASecond();
                UpdateAzureResourceUsageBackgroundLoop(subscription, location, ServiceType.Storage, updateCapacityInterval, logger);
            }

            // TODO: Candidate for RunBackgroundEnumerableAsync usage
            // Update the network capacity
            foreach (var (subscription, location) in networkSubscriptionLocations)
            {
                await WaitASecond();
                UpdateAzureResourceUsageBackgroundLoop(subscription, location, ServiceType.Network, updateCapacityInterval, logger);
            }

            // TODO: Candidate for RunBackgroundEnumerableAsync usage
            // Monitor all capacity
            var subscriptionLocationGroups = subscriptionLocations.GroupBy(item => item.location);
            foreach (var subscriptionLocationGroup in subscriptionLocationGroups)
            {
                var location = subscriptionLocationGroup.Key;
                var subscriptions = subscriptionLocationGroup.Select(item => item.subscription);

                await WaitASecond();
                MonitorAzureResourceUsageBackgroundLoop(location, subscriptions, ServiceType.Compute, monitorCapacityInterval, logger);

                await WaitASecond();
                MonitorAzureResourceUsageBackgroundLoop(location, subscriptions, ServiceType.Storage, monitorCapacityInterval, logger);

                await WaitASecond();
                MonitorAzureResourceUsageBackgroundLoop(location, subscriptions, ServiceType.Network, monitorCapacityInterval, logger);
            }
        }

        private static async Task WaitASecond()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        private void UpdateAzureResourceUsageBackgroundLoop(
            IAzureSubscription subscription,
            AzureLocation location,
            ServiceType serviceType,
            TimeSpan updateInterval,
            IDiagnosticsLogger logger)
        {
            // Each loop gets its own logger with a unique activity id.
            logger = logger
                .FromExisting(withActivityId: true)
                .WithValues(
                    new LogValueSet
                    {
                        { nameof(subscription), subscription.SubscriptionId },
                        { nameof(location), location.ToString() },
                        { nameof(serviceType), serviceType.ToString() },
                    });

            TaskHelper.RunBackgroundLoop(
                UpdateCapacityLoopName,
                (childLogger) => UpdateAzureResourceUsageAsync(subscription, location, serviceType, childLogger),
                updateInterval,
                logger);
        }

        private async Task<bool> UpdateAzureResourceUsageAsync(
            IAzureSubscription subscription,
            AzureLocation location,
            ServiceType serviceType,
            IDiagnosticsLogger logger)
        {
            var containerName = ResourceNameBuilder.GetLeaseName(UpdateCapacityLeaseContainerName);
            var leaseName = $"{UpdateCapacityLeasePrefix}-{subscription.SubscriptionId}-{location}-{serviceType}".ToLowerInvariant();
            var leaseTime = UpdateCapacityLeaseTimeSpan;
            if (DeveloperStamp)
            {
                leaseTime *= 0.1;
            }

            using (var lease = await ClaimedDistributedLease.Obtain(containerName, leaseName, leaseTime, logger))
            {
                if (lease != null)
                {
                    TaskHelper.RunBackground(
                        UpdateCapacityTaskName,
                        (childLogger) => AzureSubscriptionCapacityProvider.UpdateAzureResourceUsageAsync(subscription, location, serviceType, childLogger),
                        logger);
                }
            }

            return true;
        }

        private void MonitorAzureResourceUsageBackgroundLoop(
            AzureLocation location,
            IEnumerable<IAzureSubscription> subscriptions,
            ServiceType serviceType,
            TimeSpan monitorInterval,
            IDiagnosticsLogger logger)
        {
            // Each loop gets its own logger with a unique activity id.
            logger = logger
                .FromExisting(withActivityId: true)
                .WithValues(
                    new LogValueSet
                    {
                        { nameof(location), location.ToString() },
                        { nameof(serviceType), serviceType.ToString() },
                    });

            TaskHelper.RunBackgroundLoop(
                MonitorCapacityLoopName,
                (childLogger) => MonitorAzureResourceUsageAsync(location, subscriptions, serviceType, childLogger),
                monitorInterval,
                logger);
        }

        private async Task<bool> MonitorAzureResourceUsageAsync(
            AzureLocation location,
            IEnumerable<IAzureSubscription> subscriptions,
            ServiceType serviceType,
            IDiagnosticsLogger logger)
        {
            var containerName = ResourceNameBuilder.GetLeaseName(MonitorCapacityLeaseContainerName);
            var leaseName = $"{MonitorCapacityLeasePrefix}-{location}-{serviceType}".ToLowerInvariant();
            var leaseTime = MonitorCapacityLeaseTimeSpan;
            if (DeveloperStamp)
            {
                leaseTime *= 0.1;
            }

            using (var lease = await ClaimedDistributedLease.Obtain(containerName, leaseName, MonitorCapacityLeaseTimeSpan, logger))
            {
                if (lease != null)
                {
                    TaskHelper.RunBackground(
                        MonitorCapacityTaskName,
                        (childLogger) => MonitorAzureResourceUsageInnerAsync(location, subscriptions, serviceType, childLogger),
                        logger);
                }
            }

            return true;
        }

        private async Task MonitorAzureResourceUsageInnerAsync(
            AzureLocation location,
            IEnumerable<IAzureSubscription> subscriptions,
            ServiceType serviceType,
            IDiagnosticsLogger logger)
        {
            // Quota: (limit, current)
            var aggregate = new Dictionary<string, (long, long)>();

            // Report subscription-location quota thresholds
            foreach (var subscription in subscriptions)
            {
                try
                {
                    var resourceUsages = await AzureSubscriptionCapacityProvider
                        .GetAzureResourceUsageAsync(subscription, location, serviceType, logger);

                    foreach (var resourceUsage in resourceUsages)
                    {
                        UpdateAggregate(aggregate, resourceUsage);
                        LogCapacityAlert(
                            logger,
                            subscription.DisplayName,
                            resourceUsage.Quota,
                            resourceUsage.Limit,
                            resourceUsage.CurrentValue,
                            resourceUsage.SubscriptionId,
                            resourceUsage.Location,
                            subscription.Enabled);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogException($"{nameof(MonitorAzureResourceUsageInnerAsync)}_error", ex);
                }
            }

            // Report aggregate location quota threshold
            foreach (var quota in aggregate.Keys)
            {
                var (limit, current) = aggregate[quota];
                LogCapacityAlert(logger, "aggregate", quota, limit, current);
            }
        }

        private void UpdateAggregate(Dictionary<string, (long, long)> aggregate, AzureResourceUsage resourceUsage)
        {
            var quota = resourceUsage.Quota;

            // update aggregate
            if (!aggregate.ContainsKey(quota))
            {
                aggregate[quota] = (0, 0);
            }

            var (limit, current) = aggregate[quota];
            limit += resourceUsage.Limit;
            current += resourceUsage.CurrentValue;
            aggregate[quota] = (limit, current);
        }

        private void LogCapacityAlert(
            IDiagnosticsLogger logger,
            string subscriptionName,
            string quota,
            long limit,
            long currentValue,
            string subscriptionId = default,
            AzureLocation? location = default,
            bool? enabled = default)
        {
            var usedPercent = limit > 0 ? (double)currentValue / (double)limit : 0.0;

            IDiagnosticsLogger AddLoggerValues()
            {
                return logger
                    .FluentAddValue(nameof(subscriptionName), subscriptionName)
                    .FluentAddValue(nameof(quota), quota)
                    .FluentAddValue(nameof(limit), limit)
                    .FluentAddValue(nameof(currentValue), currentValue)
                    .FluentAddValue(nameof(usedPercent), usedPercent)
                    .FluentAddValue(nameof(subscriptionId), subscriptionId)
                    .FluentAddValue(nameof(location), location)
                    .FluentAddValue(nameof(enabled), enabled);
            }

            // Log critical and warning levels for automated alerts
            if (usedPercent >= MonitorCapacityQuotaThresholdCritical)
            {
                AddLoggerValues().LogCritical(MonitorCapacityLogAlert);
            }
            else if (usedPercent >= MonitorCapacityQuotaThreshold)
            {
                AddLoggerValues().LogWarning(MonitorCapacityLogAlert);
            }

            // Always log info level kosmos and PowerBI
            AddLoggerValues().LogInfo(MonitorCapacityLogInfo);
        }
    }
}
