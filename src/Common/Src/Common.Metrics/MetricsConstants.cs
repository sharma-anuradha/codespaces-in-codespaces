// <copyright file="MetricsConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// Constants for emitting metrics values to the default listener.
    /// </summary>
    /// <remarks>
    /// These are marked internal because they are not formally part of the contract,
    /// but are specific to <see cref="DefaultMetricsListener"/>.
    /// </remarks>
    internal static class MetricsConstants
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
        /// The metrics namespace property name.
        /// </summary>
        public static readonly string Namespace = "MetricsNamespace";

        /// <summary>
        /// The metrics event name property name.
        /// </summary>
        public static readonly string Name = "MetricsName";

        /// <summary>
        /// The metrics average property name.
        /// </summary>
        public static readonly string Average = "MetricsAverage";

        /// <summary>
        /// The metrics count property name.
        /// </summary>
        public static readonly string Count = "MetricsCount";

        /// <summary>
        /// The metrics sum property name.
        /// </summary>
        public static readonly string Sum = "MetricsSum";

        /// <summary>
        /// The metrics property name for an unknown aggregate type.
        /// </summary>
        public static readonly string Value = "MetricsValue";

        /// <summary>
        /// The metrics property name for the metrics group id.
        /// </summary>
        public static readonly string GroupId = "MetricsGroupId";

        /// <summary>
        /// The metrics property name for the aggregate timestamp.
        /// </summary>
        public static readonly string TimeStamp = "MetricsTimeStamp";
    }
}
