// <copyright file="ScopedSecretResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts
{
    /// <summary>
    /// Scoped secret result corresponding to a stored secret.
    /// </summary>
    public class ScopedSecretResult
    {
        /// <summary>
        /// Gets or sets secret Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets last modified time.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets or sets secret scope.
        /// </summary>
        public SecretScope Scope { get; set; }

        /// <summary>
        /// Gets or sets secret name.
        /// </summary>
        public string SecretName { get; set; }

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
