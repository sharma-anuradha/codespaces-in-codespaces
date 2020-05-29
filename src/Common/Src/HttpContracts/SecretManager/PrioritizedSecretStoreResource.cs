// <copyright file="PrioritizedSecretStoreResource.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Prioritized SecretStore Resource.
    /// </summary>
    public class PrioritizedSecretStoreResource
    {
        /// <summary>
        /// Gets or sets secret store priority.
        /// Smaller the value higher the priority.
        /// Secrets from higher priority secret store will override the secrets with same name but belong to a lower priority secret store.
        /// For example: A User scope secret will override a Plan scope secret with same name.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets secret store resource Id.
        /// </summary>
        public Guid ResourceId { get; set; }
    }
}
