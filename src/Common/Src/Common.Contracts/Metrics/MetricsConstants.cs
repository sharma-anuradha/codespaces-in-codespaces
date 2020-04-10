// <copyright file="MetricsConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// Constants for emitting specific metrics values.
    /// </summary>
    public static class MetricsConstants
    {
        /// <summary>
        /// The metrics event message name.
        /// </summary>
        public static readonly string EventMessage = "metrics_event";

        /// <summary>
        /// The metrics aggregate message name.
        /// </summary>
        public static readonly string AggregateMessage = "metrics_aggregate";

        /// <summary>
        /// The metrics aggregate name property name.
        /// </summary>
        public static readonly string AggregateName = "MetricsAggregateName";

        /// <summary>
        /// The metrics event name property name.
        /// </summary>
        public static readonly string EventName = "MetricsEventName";

        /// <summary>
        /// The metrics namespace property name.
        /// </summary>
        public static readonly string Namespace = "MetricsNamespace";
    }
}
