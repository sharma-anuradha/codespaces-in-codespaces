// <copyright file="ServiceType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// The Auzre serice type for capacity quotas.
    /// </summary>
    public enum ServiceType
    {
        /// <summary>
        /// Azure Compute
        /// </summary>
        Compute = 0,

        /// <summary>
        /// Azure Networking
        /// </summary>
        Network = 1,

        /// <summary>
        /// Azure Storage
        /// </summary>
        Storage = 2,

        /// <summary>
        /// Azure KeyVault.
        /// </summary>
        KeyVault = 3,
    }
}
