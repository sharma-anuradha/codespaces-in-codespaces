// <copyright file="SecretScope.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts
{
    /// <summary>
    /// Secret scopes.
    /// </summary>
    public enum SecretScope
    {
        /// <summary>
        /// Plan level shared secrets store managed by Codespaces.
        /// </summary>
        Plan = 1,

        /// <summary>
        /// User level secret store managed by Codespaces.
        /// </summary>
        User = 2,
    }
}