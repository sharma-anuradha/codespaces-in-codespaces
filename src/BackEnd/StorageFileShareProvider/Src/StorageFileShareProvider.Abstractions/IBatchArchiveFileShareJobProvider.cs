// <copyright file="IBatchArchiveFileShareJobProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions
{
    /// <summary>
    /// Batch Suspend File Share Job Provider.
    /// </summary>
    public interface IBatchArchiveFileShareJobProvider : IBatchJobProvider<BatchArchiveFileShareJobInput>
    {
        /// <summary>
        /// Prepare the file share by seeding it with the blob specified.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="destBlobUriWithSas">Target destination url.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The task info that can be used to query the task.</returns>
        Task<BatchTaskInfo> StartArchiveFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            string destBlobUriWithSas,
            IDiagnosticsLogger logger);
    }
}
