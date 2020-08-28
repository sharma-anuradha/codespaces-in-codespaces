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

        /// <summary>
        /// The Environment back-end OS disk resource type.
        /// </summary>
        OSDisk = 5,

        /// <summary>
        /// Network interface for VNet Injection.
        /// </summary>
        NetworkInterface = 6,

        /// <summary>
        /// Input queue for the VM.
        /// </summary>
        InputQueue = 7,

        /// <summary>
        /// VM's disk snapshots.
        /// </summary>
        Snapshot = 8,

        /// <summary>
        /// Pool queue forresource requests.
        /// </summary>
        PoolQueue = 9,

        /// <summary>
        /// Virtual network.
        /// </summary>
        VirtualNetwork = 10,

        /// <summary>
        /// Network security group.
        /// </summary>
        NetworkSecurityGroup = 11,
    }
}
