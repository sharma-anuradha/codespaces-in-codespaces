// <copyright file="IWatchPoolTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Defines a task which is designed to watch the pools for various changes.
    /// </summary>
    public interface IWatchPoolTask : IDisposable
    {
        /// <summary>
        /// Core task which runs through each item in the pool.
        /// </summary>
        /// <param name="claimSpan">Target claim span.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Whether the task should run again.</returns>
        Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger);
    }
}
