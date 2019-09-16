// <copyright file="IAsyncBackgroundWarmup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A service or provider that supports asynchronous background warmup.
    /// </summary>
    public interface IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Wait for the service or provider to be warmed up.
        /// This task should not throw as it is used solely to determine when warmup has completed.
        /// Instead, if the warmup does not succeed, the implementation should report that via the IHealthProvider.
        /// </summary>
        /// <param name="logger">Target Logger.</param>
        /// <returns>A task that completes when the service is warmed up.</returns>
        Task WarmupCompletedAsync(IDiagnosticsLogger logger);
    }
}
