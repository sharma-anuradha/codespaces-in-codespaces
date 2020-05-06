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
        private const string UpdateCapacityLoopPrefix = "update_capacity_loop";

        // Monitor capacity constants
        private const string MonitorCapacityLeaseContainerName = "monitor-capacity-leases";
        private const string MonitorCapacityLeasePrefix = "monitor-capacity-lease";
        private const string MonitorCapacityLoopPrefix = "monitor_capacity_loop";
        private const string MonitorCapacityOperationName = "monitor_capacity_operation";
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
        /// <param name="azureClientFactory">The azure client factory.</param>
        /// <param name="claimedDistributedLease">The distributed lease helper.</param>
        /// <param name="taskHelper">The task helper.</param>
        /// <param name="resourceNameBuilder">Resource name builder, for lease names.</param>
        /// <param name="developerPersonalStampSettings">Developer mode.</param>
        public CapacityManagerJobs(
            IAzureSubscriptionCapacityProvider azureSubscriptionCapacityProvider,
            IControlPlaneInfo controlPlaneInfo,
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            IAzureClientFactory azureClientFactory,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IResourceNameBuilder resourceNameBuilder,
            DeveloperPersonalStampSettings developerPersonalStampSettings)
        {
            Requires.NotNull(azureSubscriptionCapacityProvider, nameof(azureSubscriptionCapacityProvider));
            Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            Requires.NotNull(azureClientFactory, nameof(azureClientFactory));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
            Requires.NotNull(taskHelper, nameof(taskHelper));
            Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));

            AzureSubscriptionCapacityProvider = azureSubscriptionCapacityProvider;
            AzureSubscriptionCatalog = azureSubscriptionCatalog;
            AzureClientFactory = azureClientFactory;
            ControlPlaneInfo = controlPlaneInfo;
            ClaimedDistributedLease = claimedDistributedLease;
            TaskHelper = taskHelper;
            ResourceNameBuilder = resourceNameBuilder;
            DeveloperStamp = developerPersonalStampSettings.DeveloperStamp;
        }

        private IAzureSubscriptionCapacityProvider AzureSubscriptionCapacityProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private IAzureClientFactory AzureClientFactory { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private ITaskHelper TaskHelper { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private bool DeveloperStamp { get; }

        private Random Random { get; } = new Random();

        /// <inheritdoc/>
        public async Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            // Make sure that the required providers are registered -- or no capacity!
            RegisterArmProvidersForDataPlaneSubscriptions(logger.NewChildLogger());

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

            // Update the compute capacity
            UpdateAzureResourceUsageBackgroundLoop(ServiceType.Compute, computeSubscriptionLocations, updateCapacityInterval, logger);

            // Update the storage capacity
            await DelayBetweenLoops();
            UpdateAzureResourceUsageBackgroundLoop(ServiceType.Storage, storageSubscriptionLocations, updateCapacityInterval, logger);

            // Update the network capacity
            await DelayBetweenLoops();
            UpdateAzureResourceUsageBackgroundLoop(ServiceType.Network, networkSubscriptionLocations, updateCapacityInterval, logger);

            // Group the subscription by location for monitoring and aggregates
            var subscriptionLocationGroups = subscriptionLocations.GroupBy(item => item.location);

            // Monitor the compute capacity
            await DelayBetweenLoops();
            MonitorAzureResourceUsageBackgroundLoop(ServiceType.Compute, subscriptionLocationGroups, monitorCapacityInterval, logger);

            // Monitor the storage capacity
            await DelayBetweenLoops();
            MonitorAzureResourceUsageBackgroundLoop(ServiceType.Storage, subscriptionLocationGroups, monitorCapacityInterval, logger);

            // Monitor the network capacity
            await DelayBetweenLoops();
            MonitorAzureResourceUsageBackgroundLoop(ServiceType.Network, subscriptionLocationGroups, monitorCapacityInterval, logger);
        }

        private void RegisterArmProvidersForDataPlaneSubscriptions(IDiagnosticsLogger logger)
        {
            var dataPlaneSubscriptions = AzureSubscriptionCatalog.AzureSubscriptions.Where(sub => sub.Enabled).ToArray();
            var providers = new[]
            {
                "Microsoft.Compute",
                "Microsoft.Network",
                "Microsoft.Storage",
            };

            foreach (var subscription in dataPlaneSubscriptions)
            {
                TaskHelper.RunBackground(
                    $"register_arm_providers_for_{subscription.DisplayName}",
                    async (childLogger) =>
                    {
                        var id = Guid.Parse(subscription.SubscriptionId);
                        childLogger
                            .FluentAddBaseValue("subscriptionId", id)
                            .FluentAddBaseValue("subscriptionName", subscription.DisplayName);

                        using (var client = await AzureClientFactory.GetResourceManagementClient(id))
                        {
                            // Note: not using TaskHelper here because I need to maintain the client object between iterations.
                            var tasksWithThisClient = new List<Task>();

                            foreach (var provider in providers)
                            {
                                var task = client.Providers.RegisterWithHttpMessagesAsync(provider)
                                    .ContinueWith((t) =>
                                    {
                                        var innerLogger = childLogger.NewChildLogger();
                                        innerLogger.AddValue("provider", provider);

                                        if (t.IsFaulted)
                                        {
                                            innerLogger.LogException("register_arm_provider_error", t.Exception);
                                        }
                                        else
                                        {
                                            innerLogger.LogInfo("register_arm_provider");
                                        }
                                    });

                                tasksWithThisClient.Add(task);
                            }

                            try
                            {
                                await Task.WhenAll(tasksWithThisClient);
                            }
                            catch (Exception)
                            {
                                // Exceptions already logged in individual tasks.
                            }
                        }
                    },
                    logger);
            }
        }

        // Temporal offset to distribute inital load of recuring tasks
        private async Task DelayBetweenLoops()
        {
            await Task.Delay(Random.Next(1000, 2000));
        }

        private void UpdateAzureResourceUsageBackgroundLoop(
            ServiceType serviceType,
            IEnumerable<(IAzureSubscription subscription, AzureLocation location)> subscriptionLocations,
            TimeSpan schedule,
            IDiagnosticsLogger logger)
        {
            var loopName = UpdateCapacityLoopPrefix + $"_{serviceType}".ToLowerInvariant();

            TaskHelper.RunBackgroundLoop(
                loopName,
                async (loopLogger) =>
                {
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        loopName,
                        subscriptionLocations,
                        (item, childLogger) => UpdateAzureResourceUsageAsync(serviceType, item, childLogger),
                        loopLogger,
                        (item, childLogger) => ObtainUpdateCapacityLeaseAsync(serviceType, item, childLogger));

                    return true;
                },
                schedule,
                logger);
        }

        private async Task UpdateAzureResourceUsageAsync(ServiceType serviceType, (IAzureSubscription subscription, AzureLocation location) item, IDiagnosticsLogger logger)
        {
            var (subscription, location) = item;

            logger
                .FluentAddBaseValue(nameof(location), location.ToString())
                .FluentAddBaseValue(nameof(serviceType), serviceType.ToString());

            try
            {
                await AzureSubscriptionCapacityProvider.UpdateAzureResourceUsageAsync(
                    subscription,
                    location,
                    serviceType,
                    logger.NewChildLogger());
            }
            catch (Exception ex)
            {
                var message = $"{UpdateCapacityLoopPrefix}_{serviceType}_error".ToLowerInvariant();
                logger.LogException(message, ex);
            }
        }

        private async Task<IDisposable> ObtainUpdateCapacityLeaseAsync(ServiceType serviceType, (IAzureSubscription subscription, AzureLocation location) item, IDiagnosticsLogger logger)
        {
            var (subscription, location) = item;
            var containerName = ResourceNameBuilder.GetLeaseName(UpdateCapacityLeaseContainerName);
            var leaseName = $"{UpdateCapacityLeasePrefix}-{subscription.SubscriptionId}-{location}-{serviceType}".ToLowerInvariant();
            var leaseTime = UpdateCapacityLeaseTimeSpan;
            if (DeveloperStamp)
            {
                leaseTime *= 0.1;
            }

            return await ClaimedDistributedLease.Obtain(
                containerName,
                leaseName,
                leaseTime,
                logger);
        }

        private void MonitorAzureResourceUsageBackgroundLoop(
            ServiceType serviceType,
            IEnumerable<IGrouping<AzureLocation, (IAzureSubscription subscription, AzureLocation location)>> subscriptionLocationGroups,
            TimeSpan monitorCapacityInterval,
            IDiagnosticsLogger logger)
        {
            var loopName = MonitorCapacityLoopPrefix + $"_{serviceType}".ToLowerInvariant();

            TaskHelper.RunBackgroundLoop(
                loopName,
                async (loopLogger) =>
                {
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        loopName,
                        subscriptionLocationGroups,
                        (item, childLogger) => MonitorAzureResourceUsageAsync(serviceType, item, childLogger),
                        loopLogger,
                        (item, childLogger) => ObtainMonitorCapacityLeaseAsync(serviceType, item, childLogger));

                    return true;
                },
                monitorCapacityInterval,
                logger);
        }

        private async Task MonitorAzureResourceUsageAsync(
            ServiceType serviceType,
            IGrouping<AzureLocation, (IAzureSubscription, AzureLocation)> subscriptionLocationsGrouping,
            IDiagnosticsLogger logger)
        {
            var location = subscriptionLocationsGrouping.Key;
            var subscriptions = subscriptionLocationsGrouping
                .AsEnumerable()
                .Select(item => item.Item1);

            await logger.OperationScopeAsync(
                MonitorCapacityOperationName,
                async (innerLogger) =>
                {
                    innerLogger
                        .FluentAddBaseValue(nameof(location), location.ToString())
                        .FluentAddBaseValue(nameof(serviceType), serviceType.ToString());

                    var aggregate = new Dictionary<string, (long, long)>();

                    // Report subscription-location quota thresholds
                    foreach (var subscription in subscriptions)
                    {
                        try
                        {
                            var resourceUsages = await AzureSubscriptionCapacityProvider
                                .GetAzureResourceUsageAsync(
                                    subscription,
                                    location,
                                    serviceType,
                                    innerLogger.NewChildLogger());

                            foreach (var resourceUsage in resourceUsages)
                            {
                                UpdateAggregate(aggregate, resourceUsage);
                                LogCapacityAlert(
                                    innerLogger,
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
                            innerLogger.LogException($"{nameof(MonitorAzureResourceUsageAsync)}_error", ex);
                        }
                    }

                    // Report aggregate location quota threshold
                    foreach (var quota in aggregate.Keys)
                    {
                        var (limit, current) = aggregate[quota];
                        LogCapacityAlert(innerLogger, "aggregate", quota, limit, current);
                    }
                },
                null,
                swallowException: true);
        }

        private async Task<IDisposable> ObtainMonitorCapacityLeaseAsync(
            ServiceType serviceType,
            IEnumerable<(IAzureSubscription subscription, AzureLocation location)> items,
            IDiagnosticsLogger logger)
        {
            _ = items;
            var containerName = ResourceNameBuilder.GetLeaseName(MonitorCapacityLeaseContainerName);
            var leaseName = $"{MonitorCapacityLeasePrefix}-{serviceType}".ToLowerInvariant();
            var leaseTime = MonitorCapacityLeaseTimeSpan;
            if (DeveloperStamp)
            {
                leaseTime *= 0.1;
            }

            return await ClaimedDistributedLease.Obtain(
                containerName,
                leaseName,
                leaseTime,
                logger);
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
