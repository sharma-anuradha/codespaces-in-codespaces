// <copyright file="IWatchOrphanedSystemEnvironmentsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Watch Orphaned System Environments Task.
    /// </summary>
    public interface IWatchOrphanedSystemEnvironmentsTask
    {
        /// <summary>
        /// Task which ensures that the all environments still have their related
        /// resources.
        /// </summary>
        /// <param name="claimSpan">Target claim span.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Whether the task should run again.</returns>
        Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger);
    }
}
