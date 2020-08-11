// <copyright file="ICertificateKeyVaultSecretCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.KeyVault;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Marker interface for the Certificate Key Vault Secret Cache.
    /// </summary>
    public interface ICertificateKeyVaultSecretCache : IKeyVaultSecretCache<Certificate>
    {
    }
}
