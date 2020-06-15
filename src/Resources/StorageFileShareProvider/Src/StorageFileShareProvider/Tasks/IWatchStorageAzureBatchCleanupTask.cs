// <copyright file="IWatchStorageAzureBatchCleanupTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Tasks
{
    /// <summary>
    /// Task manager that regularly checks Azure Batch jobs and cleans them up as needed.
    /// </summary>
    public interface IWatchStorageAzureBatchCleanupTask : IDisposable
    {
        /// <summary>
        /// Core task which runs to check which resources are in a bad state.
        /// </summary>
        /// <param name="claimSpan">The interval (frequency) at which the task should be executed.</param>
        /// <param name="rootLogger">Target logger.</param>
        /// <returns>Whether the task should run again.</returns>
        Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger rootLogger);
    }
}
