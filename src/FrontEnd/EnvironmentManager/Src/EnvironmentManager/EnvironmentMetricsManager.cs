// <copyright file="EnvironmentMetricsManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// <see cref="IMetricsManager"/> extensions for <see cref="CloudEnvironment"/>.
    /// </summary>
    public class EnvironmentMetricsManager : IEnvironmentMetricsManager
    {
        private const string EnvironmentMetricsNamespace = "Microsoft.VSOnline/Environments";
        private const string EnvironmentAgeInDaysProperty = "EnvironmentAgeInDays";
        private const string EnvironmentAgeInHoursProperty = "EnvironmentAgeInHours";
        private const string EnvironmentAutoShutdownDelayMinutesProperty = "EnvironmentAutoShutdownDelayMinutes";
        private const string EnvironmentCreatedProperty = "EnvironmentCreated";
        private const string EnvironmentHasPersonalizationProperty = "EnvironmentHasPersonalization";
        private const string EnvironmentHasSeedInfoProperty = "EnvironmentHasSeedInfo";
        private const string EnvironmentIdProperty = "EnvironmentId";
        private const string EnvironmentLastActiveProperty = "EnvironmentLastActive";
        private const string EnvironmentLastStateDurationInDaysProperty = "lastStateDurationInDays";
        private const string EnvironmentLastStateDurationInHoursProperty = "lastStateDurationInHours";
        private const string EnvironmentLastStateProperty = "EnvironmentLastState";
        private const string EnvironmentLastUsedProperty = "EnvironmentLastUsed";
        private const string EnvironmentLocationProperty = "EnvironmentLocation";
        private const string EnvironmentPartnerProperty = "EnvironmentPartner";
        private const string EnvironmentStateProperty = "EnvironmentState";
        private const string EnvironmentUpdatedProperty = "EnvironmentUpdated";
        private const string SkuComputeCoresProperty = "SkuComputeCores";
        private const string SkuComputeFamilyProperty = "SkuComputeFamily";
        private const string SkuComputeOsProperty = "SkuComputeOS";
        private const string SkuNameProperty = "SkuName";
        private const string SkuStorageSizeInGbProperty = "SkuStorageSizeInGB";
        private const string SkuStorageTypeProperty = "SkuStorageType";
        private const string ClientCountryCodeProperty = "ClientCountryCode";
        private const string ClientAzureGeographyProperty = "ClientAzureGeography";
        private const string ClientVsoClientTypeProperty = "ClientVsoClientType";
        private const string EnvironmentCountMetricName = "EnvironmentCount";

        private static readonly Dictionary<CloudEnvironmentState, string> StateEventNames = new Dictionary<CloudEnvironmentState, string>
        {
            { CloudEnvironmentState.Archived, "archived" },
            { CloudEnvironmentState.Available, "available" },
            { CloudEnvironmentState.Awaiting, "awaiting" },
            { CloudEnvironmentState.Created, "created" },
            { CloudEnvironmentState.Deleted, "deleted" },
            { CloudEnvironmentState.Failed, "failed" },
            { CloudEnvironmentState.None, "none" },
            { CloudEnvironmentState.Provisioning, "provisioining" },
            { CloudEnvironmentState.Shutdown, "shutdown" },
            { CloudEnvironmentState.ShuttingDown, "shuttingDown" },
            { CloudEnvironmentState.Starting, "starting" },
            { CloudEnvironmentState.Unavailable, "unavailable" },
        };

        private static readonly Dictionary<CloudEnvironmentState, string> StateEndedEventNames = new Dictionary<CloudEnvironmentState, string>
        {
            { CloudEnvironmentState.Created, "createdStateEnded" },
            { CloudEnvironmentState.Available, "availableStateEnded" },
            { CloudEnvironmentState.Shutdown, "shutdownStateEnded" },
            { CloudEnvironmentState.Archived, "archivedStateEnded" },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentMetricsManager"/> class.
        /// </summary>
        /// <param name="metricsLogger">The metrics logger.</param>
        /// <param name="skuCatalog">The SKU catalog.</param>
        public EnvironmentMetricsManager(
            IMetricsManager metricsLogger,
            ISkuCatalog skuCatalog)
        {
            MetricsLogger = Requires.NotNull(metricsLogger, nameof(metricsLogger));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
        }

        private IMetricsManager MetricsLogger { get; }

        private ISkuCatalog SkuCatalog { get; }

        /// <inheritdoc/>
        public void PostEnvironmentEvent(
            CloudEnvironment environment,
            CloudEnvironmentStateSnapshot lastStateSnapshot,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(environment, nameof(environment));
            Requires.NotNull(lastStateSnapshot, nameof(lastStateSnapshot));
            Requires.NotNull(logger, nameof(logger));

            var newState = environment.State;
            var lastState = lastStateSnapshot.State;

            // Get the environment SKU. Skip logging if sku is unknown.
            if (!SkuCatalog.CloudEnvironmentSkus.TryGetValue(environment.SkuName, out var sku))
            {
                return;
            }

            var now = DateTime.UtcNow; // get a consistent "now"
            var ageInHours = (now - environment.Created).TotalHours;
            var ageInDays = (now - environment.Created).TotalDays;
            var lastStateStarted = lastStateSnapshot.LastStateUpdated;
            var lastStateEnded = environment.LastStateUpdated;
            var lastStateDuration = lastStateEnded - lastStateStarted;
            var lastStateDurationInHours = lastStateDuration.TotalHours;
            var lastStateDurationInDays = lastStateDuration.TotalDays;
            var isoCountryCode = environment.CreationMetrics?.IsoCountryCode;
            var azureGeography = environment.CreationMetrics?.AzureGeography;
            var vsoClientType = environment.CreationMetrics?.VsoClientType;

            var properties = new Dictionary<string, string>
            {
                // WARNING: Do not emit any EUII in to metrics!
                { EnvironmentAgeInDaysProperty, FormatValue(ageInDays) },
                { EnvironmentAgeInHoursProperty, FormatValue(ageInHours) },
                { EnvironmentAutoShutdownDelayMinutesProperty, environment.AutoShutdownDelayMinutes.ToString() },
                { EnvironmentCreatedProperty, FormatValue(environment.Created) },
                { EnvironmentHasPersonalizationProperty, FormatValue(environment.Personalization != null) },
                { EnvironmentHasSeedInfoProperty, FormatValue(environment.Seed != default) },
                { EnvironmentIdProperty, environment.Id.ToString() },
                { EnvironmentLastActiveProperty, FormatValue(environment.Active) },
                { EnvironmentLastStateDurationInDaysProperty, FormatValue(lastStateDurationInDays) },
                { EnvironmentLastStateDurationInHoursProperty, FormatValue(lastStateDurationInHours) },
                { EnvironmentLastStateProperty, lastState.ToString() },
                { EnvironmentLastUsedProperty, FormatValue(environment.LastUsed) },
                { EnvironmentLocationProperty, environment.Location.ToString() },
                { EnvironmentPartnerProperty, environment.Partner.ToString() },
                { EnvironmentStateProperty, newState.ToString() },
                { EnvironmentUpdatedProperty, FormatValue(environment.Updated) },
                { SkuComputeCoresProperty, sku.ComputeSkuCores.ToString() },
                { SkuComputeFamilyProperty, sku.ComputeSkuFamily },
                { SkuComputeOsProperty, sku.ComputeOS.ToString() },
                { SkuNameProperty, environment.SkuName },
                { SkuStorageSizeInGbProperty, sku.StorageSizeInGB.ToString() },
                { SkuStorageTypeProperty, sku.StorageSkuName },
                { ClientCountryCodeProperty, isoCountryCode },
                { ClientAzureGeographyProperty, azureGeography.ToString() },
                { ClientVsoClientTypeProperty, vsoClientType.ToString() },
            };

            // Post the state-change event.
            if (!StateEventNames.TryGetValue(newState, out var stateEventName))
            {
                MetricsLogger.PostEvent(EnvironmentMetricsNamespace, stateEventName, properties, logger);
            }
            else
            {
                logger
                    .FluentAddValue("state", newState.ToString())
                    .LogWarning("environment_metrics_logger_unknown_state_warning");
                return;
            }

            // Post the state-ended event.
            if (StateEndedEventNames.TryGetValue(lastState, out var stateEndedEventName))
            {
                MetricsLogger.PostEvent(EnvironmentMetricsNamespace, stateEndedEventName, properties, logger);
            }
        }

        /// <inheritdoc/>
        public void PostEnvironmentCount(CloudEnvironmentDimensions cloudEnvironmentDimensions, int count, IDiagnosticsLogger logger)
        {
            var dimensions = new Dictionary<string, string>
            {
                { SkuNameProperty, cloudEnvironmentDimensions.SkuName },
                { EnvironmentLocationProperty, cloudEnvironmentDimensions.Location.ToString() },
                { EnvironmentLastStateProperty, cloudEnvironmentDimensions.State.ToString() },
                { EnvironmentPartnerProperty, cloudEnvironmentDimensions.Partner.ToString() },
                { ClientCountryCodeProperty, cloudEnvironmentDimensions.IsoCountryCode },
                { ClientAzureGeographyProperty, cloudEnvironmentDimensions.AzureGeography.ToString() },
            };

            MetricsLogger.PostAggregate(EnvironmentMetricsNamespace, EnvironmentCountMetricName, AggregateType.Count, count, dimensions, logger);
        }

        private static string FormatValue(double value)
        {
            return value.ToString("f4", CultureInfo.InvariantCulture);
        }

        private static string FormatValue(DateTime value)
        {
            return value.ToString("u");
        }

        private static string FormatValue(bool value)
        {
            return value.ToString().ToLowerInvariant();
        }
    }
}
