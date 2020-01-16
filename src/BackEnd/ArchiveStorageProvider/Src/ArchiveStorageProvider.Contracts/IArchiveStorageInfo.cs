// <copyright file="IArchiveStorageInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts
{
    /// <summary>
    /// Specifies the location-coordinates and name of an azure storage account.
    /// </summary>
    public interface IArchiveStorageInfo
    {
        /// <summary>
        /// Gets the storage account azure resource location.
        /// </summary>
        IAzureResourceLocation AzureResourceLocation { get; }

        /// <summary>
        /// Gets the storage account name.
        /// </summary>
        string StorageAccountName { get; }
    }
}
