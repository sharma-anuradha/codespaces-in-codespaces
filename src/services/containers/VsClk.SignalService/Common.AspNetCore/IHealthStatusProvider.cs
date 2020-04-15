// <copyright file="IHealthStatusProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Interface to provide a state that indicate health status.
    /// </summary>
    public interface IHealthStatusProvider
    {
        /// <summary>
        /// Report the health state of this provider.
        /// </summary>
        bool IsHealthy { get; }

        /// <summary>
        /// Report a status object to be shown on our Status controller.
        /// </summary>
        object Status { get; }
    }
}
