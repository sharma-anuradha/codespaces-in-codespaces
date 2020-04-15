// <copyright file="MetricsDiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// Etensions for emitting specific metrics values to <see cref="DefaultMetricsListener"/>.
    /// </summary>
    internal static class MetricsDiagnosticsLoggerExtensions
    {
        /// <summary>
        /// Make a new child logger for emitting metrics to a <see cref="IDiagnosticsLogger"/>.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">The metric name.</param>
        /// <param name="groupId">The group id.</param>
        /// <param name="timeStamp">The timestamp.</param>
        /// <returns>A new <see cref="IDiagnosticsLogger"/> instance.</returns>
        public static IDiagnosticsLogger NewMetricsChildLogger(
            this IDiagnosticsLogger logger,
            string metricNamespace,
            string metricName,
            Guid? groupId,
            DateTime? timeStamp)
        {
            var metricsLogger = logger.NewChildLogger()
                .AddMetricsNamespace(metricNamespace)
                .AddMetricsName(metricName)
                .AddMetricsGroupId(groupId ?? Guid.NewGuid())
                .AddMetricsTimestamp(timeStamp ?? DateTime.UtcNow);
            return metricsLogger;
        }

        /// <summary>
        /// Add the metrics event name.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The logging value.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddMetricsName(this IDiagnosticsLogger logger, string value)
        {
            return logger.FluentAddValue(MetricsConstants.Name, value);
        }

        /// <summary>
        /// Add the metrics namespace.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The logging value.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddMetricsNamespace(this IDiagnosticsLogger logger, string value)
        {
            return logger.FluentAddValue(MetricsConstants.Namespace, value);
        }

        /// <summary>
        /// Add the metrics aggregate average.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The logging value.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddMetricsAverage(this IDiagnosticsLogger logger, double value)
        {
            return logger.FluentAddValue(MetricsConstants.Average, value);
        }

        /// <summary>
        /// Add the metrics aggregate count.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The logging value.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddMetricsCount(this IDiagnosticsLogger logger, double value)
        {
            return logger.FluentAddValue(MetricsConstants.Count, value);
        }

        /// <summary>
        /// Add the metrics aggregate sum.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The logging value.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddMetricsSum(this IDiagnosticsLogger logger, double value)
        {
            return logger.FluentAddValue(MetricsConstants.Sum, value);
        }

        /// <summary>
        /// Add the metrics aggregate value.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The logging value.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddMetricsValue(this IDiagnosticsLogger logger, double value)
        {
            return logger.FluentAddValue(MetricsConstants.Value, value);
        }

        /// <summary>
        /// Add the metrics aggregate id.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The logging value.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddMetricsGroupId(this IDiagnosticsLogger logger, Guid? value)
        {
            return logger.FluentAddValue(MetricsConstants.GroupId, value ?? Guid.NewGuid());
        }

        /// <summary>
        /// Add the metrics timestamp.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The logging value.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddMetricsTimestamp(this IDiagnosticsLogger logger, DateTime? value)
        {
            return logger.FluentAddValue(MetricsConstants.TimeStamp, (value ?? DateTime.UtcNow).ToString("u"));
        }
    }
}
