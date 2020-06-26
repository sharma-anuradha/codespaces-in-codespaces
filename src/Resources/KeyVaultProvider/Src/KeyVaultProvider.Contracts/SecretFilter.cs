// <copyright file="SecretFilter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts
{
    /// <summary>
    /// Secret filter.
    /// </summary>
    public class SecretFilter
    {
        /// <summary>
        /// Gets or sets secret filter type.
        /// </summary>
        public SecretFilterType Type { get; set; }

        /// <summary>
        /// Gets or sets filter value.
        /// </summary>
        public string Value { get; set; }
    }
}
