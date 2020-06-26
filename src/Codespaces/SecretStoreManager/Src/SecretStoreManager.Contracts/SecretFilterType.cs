// <copyright file="SecretFilterType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts
{
    /// <summary>
    /// Secret filter type.
    /// </summary>
    public enum SecretFilterType
    {
        /// <summary>
        /// Git repo.
        /// </summary>
        GitRepo = 1,

        /// <summary>
        /// Codespace name.
        /// </summary>
        CodespaceName = 2,
    }
}