// <copyright file="IBatchJobProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions
{
    /// <summary>
    /// Batch Job Provider definition.
    /// </summary>
    /// <typeparam name="T">Type of input.</typeparam>
    public interface IBatchJobProvider<T>
        where T : BatchTaskInput
    {
        /// <summary>
        /// Prepare the file share by seeding it with the blob specified.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="taskInput">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The task info that can be used to query the task.</returns>
        Task<BatchTaskInfo> StartBatchTaskAsync(
            AzureResourceInfo azureResourceInfo,
            T taskInput,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Check if the batch task has completed.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="taskInfo">The info for the task preparing the file share.</param>
        /// <param name="maxWaitTime">Timeout for how long a task can be in a non-running state.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Status.</returns>
        Task<BatchTaskStatus> CheckBatchTaskStatusAsync(
            AzureResourceInfo azureResourceInfo,
            BatchTaskInfo taskInfo,
            TimeSpan maxWaitTime,
            IDiagnosticsLogger logger);
    }
}
