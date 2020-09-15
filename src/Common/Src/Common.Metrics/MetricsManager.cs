// <copyright file="MetricsManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Azure.Maps;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// The business metrics manager. Emits events to listeners.
    /// </summary>
    public class MetricsManager : IMetricsManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsManager"/> class.
        /// </summary>
        /// <param name="metricsListeners">The list of registered metrics listeners.</param>
        /// <param name="azureMapsClient">The azure maps client.</param>
        /// <param name="memoryCache">The in-memory cache.</param>
        public MetricsManager(
            IEnumerable<IMetricsListener> metricsListeners,
            IAzureMapsClient azureMapsClient,
            IManagedCache memoryCache)
        {
            Requires.NotNullOrEmpty(metricsListeners, nameof(metricsListeners));
            Requires.NotNull(azureMapsClient, nameof(azureMapsClient));
            Requires.NotNull(memoryCache, nameof(memoryCache));

            MetricsListeners = metricsListeners.ToArray();
            AzureMapsClient = azureMapsClient;
            ManagedCache = memoryCache;
        }

        private IEnumerable<IMetricsListener> MetricsListeners { get; }

        private IAzureMapsClient AzureMapsClient { get; }

        private IManagedCache ManagedCache { get; }

        /// <inheritdoc/>
        public void PostEvent(
            string metricNamespace,
            string metricEventName,
            IDictionary<string, string> eventProperties,
            Guid? groupId,
            DateTime? timeStamp,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(metricNamespace, nameof(metricNamespace));
            Requires.NotNullOrEmpty(metricEventName, nameof(metricEventName));
            Requires.NotNull(logger, nameof(logger));

            foreach (var listener in MetricsListeners)
            {
                try
                {
                    listener.PostEvent(metricNamespace, metricEventName, eventProperties, groupId, timeStamp, logger);
                }
                catch (Exception ex)
                {
                    logger.FluentAddValue("ListenerName", listener.Name)
                        .LogException("metrics_manager_post_event_error", ex);

                    // next listener...
                }
            }
        }

        /// <inheritdoc/>
        public void PostAggregate(
            string metricNamespace,
            string metricAggregateName,
            AggregateType aggregateType,
            int aggregateValue,
            IDictionary<string, string> aggregateDimensions,
            Guid? groupId,
            DateTime? timeStamp,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(metricNamespace, nameof(metricNamespace));
            Requires.NotNullOrEmpty(metricAggregateName, nameof(metricAggregateName));
            Requires.NotNull(aggregateDimensions, nameof(aggregateDimensions));
            Requires.NotNull(logger, nameof(logger));

            foreach (var listener in MetricsListeners)
            {
                try
                {
                    listener.PostAggregate(metricNamespace, metricAggregateName, aggregateType, aggregateValue, aggregateDimensions, groupId, timeStamp, logger);
                }
                catch (Exception ex)
                {
                    logger.FluentAddValue("ListenerName", listener.Name)
                        .LogException("metrics_manager_post_aggregate_error", ex);

                    // next listener...
                }
            }
        }

        /// <inheritdoc/>
        public async Task<MetricsClientInfo> GetMetricsInfoForRequestAsync(
            IHeaderDictionary requestHeaders,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(requestHeaders, nameof(requestHeaders));
            Requires.NotNull(logger, nameof(logger));

            // The client IP address is passed in the X-forwarded-for header.
            const string ForwardedForHeader = "X-forwarded-for";

            // Get the country code from the IP address.
            var isoCountryCode = default(string);
            if (requestHeaders.ContainsKey(ForwardedForHeader))
            {
                var clientIpAddress = (string)requestHeaders[ForwardedForHeader];
                if (!string.IsNullOrEmpty(clientIpAddress))
                {
                    var cacheKey = $"ipaddresstolocation:{clientIpAddress}";

                    var ipAddressToLocationResult = await ManagedCache.GetAsync<IpAddressToLocationResult>(cacheKey, logger.NewChildLogger());
                    if (ipAddressToLocationResult == null)
                    {
                        try
                        {
                            ipAddressToLocationResult = await AzureMapsClient.GetGeoLocationAsync(clientIpAddress, logger.NewChildLogger());
                            await ManagedCache.SetAsync(cacheKey, ipAddressToLocationResult, TimeSpan.FromHours(1), logger.NewChildLogger());
                        }
                        catch (Exception ex)
                        {
                            // Logging it as a warning & continue without location data...
                            logger.LogWarning("metrics_manager_error", ex);
                        }
                    }

                    // Get the country code if available
                    isoCountryCode = ipAddressToLocationResult?.CountryRegion?.IsoCode;
                }
            }

            // Determine the VSO client type from the user agent header.
            var vsoClientType = default(VsoClientType?);
            if (requestHeaders.ContainsKey(HttpConstants.UserAgentHeader))
            {
                var userAgent = (string)requestHeaders[HttpConstants.UserAgentHeader];
                vsoClientType = MetricsUtilities.UserAgentToVsoClientType(userAgent);
            }

            return new MetricsClientInfo(isoCountryCode, vsoClientType);
        }
    }
}
