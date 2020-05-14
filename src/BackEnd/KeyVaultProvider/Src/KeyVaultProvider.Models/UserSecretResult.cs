// <copyright file="UserSecretResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models
{
    /// <summary>
    /// User secret result.
    /// </summary>
    public class UserSecretResult
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
        /// Gets or sets secret name.
        /// </summary>
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets secret type.
        /// </summary>
        public SecretType Type { get; set; }

        /// <summary>
        /// Gets or sets secret filters.
        /// </summary>
        public IDictionary<SecretFilterType, string> Filters { get; set; }
    }
}