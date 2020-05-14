// <copyright file="UpdateSecretInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models
{
    /// <summary>
    /// Input for updating a secret.
    /// </summary>
    public class UpdateSecretInput
    {
        /// <summary>
        /// Gets or sets secret name.
        /// </summary>
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets secret value.
        /// </summary>
        public string Value { get; set; }
    }
}