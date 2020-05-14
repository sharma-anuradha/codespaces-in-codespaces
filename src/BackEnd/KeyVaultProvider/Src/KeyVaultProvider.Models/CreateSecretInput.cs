// <copyright file="CreateSecretInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Create secret input.
    /// </summary>
    public class CreateSecretInput
    {
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
        /// Gets or sets secret filters.
        /// </summary>
        public IDictionary<SecretFilterType, string> Filters { get; set; }
    }
}