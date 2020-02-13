// <copyright file="RelayServiceMetrics.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Define the service metrics.
    /// </summary>
    public struct RelayServiceMetrics
    {
        public RelayServiceMetrics(
            int hubCount)
        {
            Count = hubCount;
        }

        public int Count { get; }
    }
}
