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

        /// <summary>
        /// The metrics average property name.
        /// </summary>
        public static readonly string AggregateAverage = "MetricsAverage";

        /// <summary>
        /// The metrics count property name.
        /// </summary>
        public static readonly string AggregateCount = "MetricsCount";

        /// <summary>
        /// The metrics sum property name.
        /// </summary>
        public static readonly string AggregateSum = "MetricsSum";

        /// <summary>
        /// The metrics property name for an unknown aggregate type.
        /// </summary>
        public static readonly string AggregateOther = "MetricsValue";
    }
}
