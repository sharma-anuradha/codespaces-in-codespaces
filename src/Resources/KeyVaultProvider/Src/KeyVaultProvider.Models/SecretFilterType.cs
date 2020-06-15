// <copyright file="SecretFilterType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models
{
    /// <summary>
    /// Secret filter types.
    /// </summary>
    public enum SecretFilterType
    {
        /// <summary>
        /// Git repo filter glob.
        /// </summary>
        GitRepo = 1,

        /// <summary>
        /// Codespace name.
        /// </summary>
        CodespaceName = 2,
    }
}