// <copyright file="IMetricsListener.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// A metrics logger.
    /// </summary>
    public interface IMetricsListener
    {
        /// <summary>
        /// Gets the name o\of the listener (for internal housekeeping, and exception logging).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Post a metrics event.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">The metric event name.</param>
        /// <param name="eventProperties">The event properties.</param>
        /// <param name="groupId">The common id for a set of related aggregate values.</param>
        /// <param name="timeStamp">The common time stamp for a set of related aggregate values.</param>
        /// <param name="logger">The diagnostics logger (for operational logging, not metrics logging.).</param>
        void PostEvent(
            string metricNamespace,
            string metricName,
            IDictionary<string, string> eventProperties,
            Guid? groupId,
            DateTime? timeStamp,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Post an aggregate metric, for example, a count, sum, or average of some activity or resoruce usage.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">The metric aggregate name.</param>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="aggregateValue">The aggregate value.</param>
        /// <param name="aggregateDimensions">The aggregate dimensions.</param>
        /// <param name="groupId">The common id for a set of related aggregate values.</param>
        /// <param name="timeStamp">The common time stamp for a set of related aggregate values.</param>
        /// <param name="logger">The diagnostics logger (for operational logging, not metrics logging.).</param>
        void PostAggregate(
            string metricNamespace,
            string metricName,
            AggregateType aggregateType,
            int aggregateValue,
            IDictionary<string, string> aggregateDimensions,
            Guid? groupId,
            DateTime? timeStamp,
            IDiagnosticsLogger logger);
    }
}
