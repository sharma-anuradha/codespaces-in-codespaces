// <copyright file="ResourceType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// A Cloud Environment back-end resource type.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// The Environment back-end compute vm resource type.
        /// </summary>
        ComputeVM = 1,

        /// <summary>
        /// The Environment hot back-end storage file share resource type.
        /// </summary>
        StorageFileShare = 2,

        /// <summary>
        /// The Environment cold back-end storage blob resource type.
        /// </summary>
        StorageArchive = 3,

        /// <summary>
        /// The keyvault resource type.
        /// </summary>
        KeyVault = 4,
    }
}
