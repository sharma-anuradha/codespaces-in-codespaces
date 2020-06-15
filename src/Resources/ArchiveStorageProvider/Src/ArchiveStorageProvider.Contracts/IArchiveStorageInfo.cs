// <copyright file="IArchiveStorageInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts
{
    /// <summary>
    /// Specifies the location-coordinates and name of an azure storage account.
    /// </summary>
    public interface IArchiveStorageInfo
    {
        /// <summary>
        /// Gets the storage account azure resource info.
        /// </summary>
        AzureResourceInfo AzureResourceInfo { get; }

        /// <summary>
        /// Gets the azure location for this storage resource.
        /// </summary>
        AzureLocation AzureLocation { get; }

        /// <summary>
        /// Gets the storage account key for the <see cref="AzureResourceInfo"/>.
        /// </summary>
        string StorageAccountKey { get; }
    }
}
