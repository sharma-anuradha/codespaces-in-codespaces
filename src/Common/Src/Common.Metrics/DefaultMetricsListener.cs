// <copyright file="DefaultMetricsListener.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// A metrics logger that logs to the diagnostics logger.
    /// </summary>
    /// <remarks>
    /// Kusto telemetry is OK for generalized view of the world.
    /// Not robust enough for all-up historical tracking of the world.
    /// </remarks>
    public class DefaultMetricsListener : IMetricsListener
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMetricsListener"/> class.
        /// </summary>
        /// <param name="options">The metrics logger options.</param>
        /// <param name="diagnosticsLoggerFactory">The logger factory.</param>
        /// <param name="logValues">The default log values.</param>
        public DefaultMetricsListener(
            IOptions<DefaultMetricsListenerOptions> options,
            IDiagnosticsLoggerFactory diagnosticsLoggerFactory,
            LogValueSet logValues)
        {
            Requires.NotNullOrEmpty(options?.Value?.MdsdEventSource, nameof(options.Value.MdsdEventSource));
            Requires.NotNull(diagnosticsLoggerFactory, nameof(diagnosticsLoggerFactory));

            // The underlying MDSD configuration routes logging events based on the "Service" property.
            // Use LogValueSet.Concat to allocate to new set, rather than modifying the global default log options.
            var metricsLogValues = new LogValueSet { { LoggingConstants.Service, options.Value.MdsdEventSource } };
            metricsLogValues = logValues?.Concat(metricsLogValues) ?? metricsLogValues;
            MetricsDiagnosticsLogger = diagnosticsLoggerFactory.New(metricsLogValues);
        }

        /// <inheritdoc/>
        public string Name => typeof(IDiagnosticsLogger).Namespace;

        private IDiagnosticsLogger MetricsDiagnosticsLogger { get; }

        /// <inheritdoc/>
        public void PostEvent(
            string metricNamespace,
            string metricName,
            IDictionary<string, string> eventProperties,
            Guid? groupId,
            DateTime? timeStamp,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(metricNamespace, nameof(metricNamespace));
            Requires.NotNullOrEmpty(metricName, nameof(metricName));
            Requires.NotNull(logger, nameof(logger));

            // Add namespace and event name
            var metricsLogger = MetricsDiagnosticsLogger.NewMetricsChildLogger(metricNamespace, metricName, groupId, timeStamp);

            // Add event properties, if specified
            if (eventProperties != null)
            {
                foreach (var item in eventProperties)
                {
                    metricsLogger.AddValue(item.Key, item.Value);
                }
            }

            // Emit to the logger.
            metricsLogger.LogInfo(MetricsConstants.EventMessage);
        }

        /// <inheritdoc/>
        public void PostAggregate(
            string metricNamespace,
            string metricName,
            AggregateType aggregateType,
            int aggregateValue,
            IDictionary<string, string> aggregateDimensions,
            Guid? groupId,
            DateTime? timeStamp,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(metricNamespace, nameof(metricNamespace));
            Requires.NotNullOrEmpty(metricName, nameof(metricName));
            Requires.NotNull(aggregateDimensions, nameof(aggregateDimensions));
            Requires.NotNull(logger, nameof(logger));

            // Add namespace and metric name, and unique aggregate group identifiers
            var metricsLogger = MetricsDiagnosticsLogger.NewMetricsChildLogger(metricNamespace, metricName, groupId, timeStamp);

            // Add dimensions
            foreach (var item in aggregateDimensions)
            {
                metricsLogger.AddValue(item.Key, item.Value);
            }

            // Add the aggregate value
            switch (aggregateType)
            {
                case AggregateType.Average:
                    metricsLogger.AddMetricsAverage(aggregateValue);
                    break;

                case AggregateType.Count:
                    metricsLogger.AddMetricsCount(aggregateValue);
                    break;

                case AggregateType.Sum:
                    metricsLogger.AddMetricsSum(aggregateValue);
                    break;

                default:
                    metricsLogger.AddMetricsValue(aggregateValue);
                    break;
            }

            // Emit to the logger.
            metricsLogger.LogInfo(MetricsConstants.AggregateMessage);
        }
    }
}
