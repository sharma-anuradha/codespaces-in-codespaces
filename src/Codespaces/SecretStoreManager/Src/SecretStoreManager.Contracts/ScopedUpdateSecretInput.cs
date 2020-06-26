// <copyright file="ScopedUpdateSecretInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts
{
    /// <summary>
    /// Scoped update secret input.
    /// </summary>
    public class ScopedUpdateSecretInput
    {
        /// <summary>
        /// Gets or sets secret scope.
        /// This is used to determine which secret store the secret lives in.
        /// </summary>
        public SecretScope Scope { get; set; }

        /// <summary>
        /// Gets or sets secret name to update.
        /// </summary>
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets secret value to update.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets notes.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets secret filters to update.
        /// </summary>
        public IEnumerable<SecretFilter> Filters { get; set; }
    }
}
