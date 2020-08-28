// <copyright file="AzureResourceQuotaNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// A resource location criterion.
    /// </summary>
    public static class AzureResourceQuotaNames
    {
        /// <summary>
        /// Quota name for VM cores.
        /// </summary>
        public const string Cores = "cores";

        /// <summary>
        /// Quota name for storage accounts.
        /// </summary>
        public const string StorageAccounts = "StorageAccounts";

        /// <summary>
        /// Quota name for virtual networks.
        /// </summary>
        public const string VirtualNetworks = "VirtualNetworks";

        /// <summary>
        /// Quota name for key vaults.
        /// </summary>
        /// <remarks>
        /// Unlike the other names, this name isn't given by Azure. It is internal to this service only.
        /// </remarks>
        public const string KeyVaults = "KeyVault";
    }
}
