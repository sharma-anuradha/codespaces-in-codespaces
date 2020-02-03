// <copyright file="IJwtCertificateCredentialsKeyVaultCacheFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Factory wrapper for creating new <see cref="JwtCertificateCredentialsKeyVaultCache="/> from global settings.
    /// </summary>
    public interface IJwtCertificateCredentialsKeyVaultCacheFactory
    {
        /// <summary>
        /// Creates a new <see cref="JwtCertificateCredentialsKeyVaultCache="/> from global settings.
        /// </summary>
        /// <param name="certName">The certificate name.</param>
        /// <param name="startPeriodicRefreshes">If true, will initial periodic refreshes on the returned cache.</param>
        /// <param name="keyVaultName">Name of the KeyVault storing the certificate.  If not set, the environment's default KeyVault is used.</param>
        /// <returns>A <see cref="JwtCertificateCredentialsKeyVaultCache="/>.</returns>
        JwtCertificateCredentialsKeyVaultCache New(string certName, bool startPeriodicRefreshes = true, string keyVaultName = null);
    }
}
