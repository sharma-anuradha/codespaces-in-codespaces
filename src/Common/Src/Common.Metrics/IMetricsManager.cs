// <copyright file="IMetricsManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// A metrics logger.
    /// </summary>
    public interface IMetricsManager
    {
        /// <summary>
        /// Post a metrics event.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricEventName">The metric event name.</param>
        /// <param name="eventProperties">The event properties.</param>
        /// <param name="groupId">The common id for a set of related aggregate values.</param>
        /// <param name="timeStamp">The common time stamp for a set of related aggregate values.</param>
        /// <param name="logger">The diagnostics logger (for operational logging, not metrics logging.).</param>
        void PostEvent(
            string metricNamespace,
            string metricEventName,
            IDictionary<string, string> eventProperties,
            Guid? groupId,
            DateTime? timeStamp,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Post an aggregate metric, for example, a count, sum, or average of some activity or resoruce usage.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricAggregateName">The metric aggregate name.</param>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="aggregateValue">The aggregate value.</param>
        /// <param name="aggregateDimensions">The aggregate dimensions.</param>
        /// <param name="groupId">The common id for a set of related aggregate values.</param>
        /// <param name="timeStamp">The common time stamp for a set of related aggregate values.</param>
        /// <param name="logger">The diagnostics logger (for operational logging, not metrics logging.).</param>
        void PostAggregate(
            string metricNamespace,
            string metricAggregateName,
            AggregateType aggregateType,
            int aggregateValue,
            IDictionary<string, string> aggregateDimensions,
            Guid? groupId,
            DateTime? timeStamp,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Get metrics information for the given http request.
        /// </summary>
        /// <param name="requestHeaders">The request headers.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The tupple of country code, azure geo, and VSO client type.</returns>
        Task<MetricsClientInfo> GetMetricsInfoForRequestAsync(
            IHeaderDictionary requestHeaders,
            IDiagnosticsLogger logger);
    }
}
