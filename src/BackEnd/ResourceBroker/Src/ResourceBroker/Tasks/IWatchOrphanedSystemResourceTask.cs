// <copyright file="IWatchOrphanedSystemResourceTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for Orphaned System Resource.
    /// </summary>
    public interface IWatchOrphanedSystemResourceTask : IDisposable
    {
        /// <summary>
        /// Core task which runs through all the system resources that haven't received their keep alive
        /// ping from the `IWatchOrphanedAzureResourceTask` task (which updates the keep alive for all
        /// system resources for which we have azure resources).
        /// </summary>
        /// <param name="claimSpan">Target claim span.</param>
        /// <param name="rootLogger">Target logger.</param>
        /// <returns>Whether the task should run again.</returns>
        Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger rootLogger);
    }
}
