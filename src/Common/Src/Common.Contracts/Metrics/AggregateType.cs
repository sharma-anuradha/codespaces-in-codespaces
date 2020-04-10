// <copyright file="AggregateType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// The aggregate type for an aggregate metric.
    /// </summary>
    public enum AggregateType
    {
        /// <summary>
        /// The aggregate represents a count of items.
        /// </summary>
        Count,

        /// <summary>
        /// The aggregate represents a sum.
        /// </summary>
        Sum,

        /// <summary>
        /// The aggregate represents an average.
        /// </summary>
        Average,
    }
}
