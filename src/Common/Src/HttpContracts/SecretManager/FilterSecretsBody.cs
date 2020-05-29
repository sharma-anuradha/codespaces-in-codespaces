// <copyright file="FilterSecretsBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Input required to filter secrets in keyvault resources.
    /// </summary>
    public class FilterSecretsBody
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
