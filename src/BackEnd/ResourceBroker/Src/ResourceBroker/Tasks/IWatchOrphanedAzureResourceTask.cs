// <copyright file="IWatchOrphanedAzureResourceTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for Orphaned Azure Resource.
    /// </summary>
    public interface IWatchOrphanedAzureResourceTask : IDisposable
    {
        /// <summary>
        /// Core task which runs through all the azure resource in a given subscription/resource group.
        /// </summary>
        /// <param name="claimSpan">Target claim span.</param>
        /// <param name="rootLogger">Target logger.</param>
        /// <returns>Whether the task should run again.</returns>
        Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger rootLogger);
    }
}
