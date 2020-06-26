// <copyright file="IBatchPrepareFileShareJobProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
{
    /// <summary>
    /// Batch Prepare File Share Job Provider definition.
    /// </summary>
    public interface IBatchPrepareFileShareJobProvider : IBatchJobProvider<BatchPrepareFileShareJobInput>
    {
        /// <summary>
        /// Prepare the file share by seeding it with the blob specified.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="storageCopyItems">Array of storage items to copy.</param>
        /// <param name="storageSizeInGb">Azure storage size in GB.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The task info that can be used to query the task.</returns>
        Task<BatchTaskInfo> StartPrepareFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            IEnumerable<StorageCopyItem> storageCopyItems,
            int storageSizeInGb,
            IDiagnosticsLogger logger);
    }
}
