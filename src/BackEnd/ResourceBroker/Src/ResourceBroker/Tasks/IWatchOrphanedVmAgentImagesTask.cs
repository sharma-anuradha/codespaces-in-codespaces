// <copyright file="IWatchOrphanedVmAgentImagesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// IWatchOrphanedArtifactImagesTask to delete obsolete artifacts (VSO agent images/blobs).
    /// </summary>
    public interface IWatchOrphanedVmAgentImagesTask : IBackgroundTask
    {
        /// <summary>
        /// Gets the respective container name for storage/VM agent images/blobs.
        /// </summary>
        /// <returns>A Task representing the completion of the processing logic.</returns>
        string GetContainerName();

        /// <summary>
        /// Gets the account name & Key for the storage account.
        /// </summary>
        /// <param name="logger">The logger which should be used.</param>
        /// <returns>A Task representing the completion of the processing logic.</returns>
        Task<IEnumerable<ShareConnectionInfo>> GetStorageAccountsAsync();

        /// <summary>
        /// Gets all the Images/Blobs that are being currently used form SkuCatalog.
        /// </summary>
        /// <param name="logger">The logger which should be used.</param>
        /// <returns>A Task representing the completion of the processing logic.</returns>
        Task<IEnumerable<string>> GetActiveImagesAsync(IDiagnosticsLogger logger);
    }
}
