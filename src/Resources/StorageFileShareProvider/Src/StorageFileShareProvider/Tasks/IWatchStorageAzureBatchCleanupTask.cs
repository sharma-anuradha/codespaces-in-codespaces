// <copyright file="IWatchStorageAzureBatchCleanupTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Tasks
{
    /// <summary>
    /// Task manager that regularly checks Azure Batch jobs and cleans them up as needed.
    /// </summary>
    public interface IWatchStorageAzureBatchCleanupTask : IBackgroundTask
    {
    }
}
