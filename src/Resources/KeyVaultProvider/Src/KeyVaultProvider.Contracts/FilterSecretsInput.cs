// <copyright file="FilterSecretsInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts
{
    /// <summary>
    /// Input required to filter secrets in keyvault resources.
    /// </summary>
    public class FilterSecretsInput
    {
        /// <summary>
        /// Gets or sets filter data.
        /// </summary>
        public IEnumerable<SecretFilterData> FilterData { get; set; }

        /// <summary>
        /// Gets or sets prioritized secret store resources.
        /// </summary>
        public IEnumerable<PrioritizedSecretStoreResource> PrioritizedSecretStoreResources { get; set; }
    }
}
