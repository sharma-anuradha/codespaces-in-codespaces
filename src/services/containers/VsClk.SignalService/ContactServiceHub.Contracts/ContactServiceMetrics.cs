// <copyright file="ContactServiceMetrics.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Define the service metrics.
    /// </summary>
    public struct ContactServiceMetrics
    {
        public ContactServiceMetrics(
            int count,
            int selfCount,
            int totalSelfCount,
            int stubCount)
        {
            Count = count;
            SelfCount = selfCount;
            TotalSelfCount = totalSelfCount;
            StubCount = stubCount;
        }

        /// <summary>
        /// Gets the total number of contacts that registered but not necesarily connected
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Number of distinct contacts that are connected
        /// </summary>
        public int SelfCount { get; }

        /// <summary>
        /// Number of total sessions being connected
        /// </summary>
        public int TotalSelfCount { get; }

        /// <summary>
        /// Number of 'stub' being created that were asked during subscription but were not found
        /// and are waiting to be resolved
        /// </summary>
        public int StubCount { get; }
    }
}
