// <copyright file="PrioritizedSecretStoreResource.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts
{
    /// <summary>
    /// Prioritized SecretStore Resource.
    /// </summary>
    public class PrioritizedSecretStoreResource
    {
        /// <summary>
        /// Gets or sets secret store priority.
        /// Smaller the value higher the priority.
        /// Secrets from higher priority resource(keyvault) will override the secrets with same name,
        ///     but belong to a lower priority resource(keyvault).
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets the keyvault resource Id.
        /// </summary>
        public Guid ResourceId { get; set; }
    }
}