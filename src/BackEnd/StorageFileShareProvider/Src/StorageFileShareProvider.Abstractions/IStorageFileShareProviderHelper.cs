// <copyright file="IStorageFileShareProviderHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions
{
    /// <summary>
    /// Represents helper operations required by the <see cref="StorageFileShareProvider"/> class.
    /// </summary>
    public interface IStorageFileShareProviderHelper
    {
        /// <summary>
        /// Create an Azure Storage account.
        /// </summary>
        /// <param name="azureSubscriptionId">Azure subscription id to create storage account in.</param>
        /// <param name="azureRegion">Azure region to create storage account in.</param>
        /// <param name="azureResourceGroup">Azure resource group to create storage account in.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Azure Resource Id of the storage account.</returns>
        Task<string> CreateStorageAccountAsync(string azureSubscriptionId, string azureRegion, string azureResourceGroup, IDiagnosticsLogger logger);

        /// <summary>
        /// Create a file share in the provided storage account.
        /// </summary>
        /// <param name="azureStorageAccountId">Azure Resource Id of the storage account.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task CreateFileShareAsync(string azureStorageAccountId, IDiagnosticsLogger logger);

        /// <summary>
        /// Prepare the file share by seeding it with the blob specified.
        /// </summary>
        /// <param name="azureStorageAccountId">Azure Resource Id of the storage account.</param>
        /// <param name="srcBlobUrl">Full url to blob to use (including SAS token with read permission to blob).</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task StartPrepareFileShareAsync(string azureStorageAccountId, string srcBlobUrl, IDiagnosticsLogger logger);

        /// <summary>
        /// Check if the preparation of the file share has completed.
        /// </summary>
        /// <param name="azureStorageAccountId">Azure Resource Id of the storage account.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The percentage completed (from 0 - 1).</returns>
        Task<double> CheckPrepareFileShareAsync(string azureStorageAccountId, IDiagnosticsLogger logger);

        /// <summary>
        /// Provides the connection information needed to connect to the file share.
        /// </summary>
        /// <param name="azureStorageAccountId">Azure Resource Id of the storage account.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The connection information for the share.</returns>
        Task<ShareConnectionInfo> GetConnectionInfoAsync(string azureStorageAccountId, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete the storage account.
        /// </summary>
        /// <param name="azureStorageAccountId">Azure Resource Id of the storage account.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task DeleteStorageAccountAsync(string azureStorageAccountId, IDiagnosticsLogger logger);
    }
}
