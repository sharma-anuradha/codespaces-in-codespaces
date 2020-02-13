// <copyright file="IHealthStatusProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Interface to provide a state that indicate health status
    /// </summary>
    public interface IHealthStatusProvider
    {
        /// <summary>
        /// Return the health state.
        /// </summary>
        bool State { get; }
    }
}
