// <copyright file="ScopedCreateSecretInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Models
{
    /// <summary>
    /// Scoped create secret input.
    /// </summary>
    public class ScopedCreateSecretInput
    {
        /// <summary>
        /// Gets or sets secret scope.
        /// </summary>
        public SecretScope Scope { get; set; }

        /// <summary>
        /// Gets or sets secret name.
        /// </summary>
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets secret value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets secret type.
        /// </summary>
        public SecretType Type { get; set; }

        /// <summary>
        /// Gets or sets notes.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets secret filters.
        /// </summary>
        public IEnumerable<SecretFilter> Filters { get; set; }
    }
}