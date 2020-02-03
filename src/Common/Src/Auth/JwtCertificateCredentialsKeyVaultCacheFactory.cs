// <copyright file="JwtCertificateCredentialsKeyVaultCacheFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Factory wrapper for creating new <see cref="JwtCertificateCredentialsKeyVaultCache="/> from global settings.
    /// </summary>
    public class JwtCertificateCredentialsKeyVaultCacheFactory : IJwtCertificateCredentialsKeyVaultCacheFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JwtCertificateCredentialsKeyVaultCacheFactory"/> class.
        /// </summary>
        /// <param name="keyVaultSecretReader">The key vault reader.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="authenticationSettings">The authentication settings.</param>
        /// <param name="logger">The logger.</param>
        public JwtCertificateCredentialsKeyVaultCacheFactory(
            IKeyVaultSecretReader keyVaultSecretReader,
            IControlPlaneInfo controlPlaneInfo,
            AuthenticationSettings authenticationSettings,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(keyVaultSecretReader, nameof(keyVaultSecretReader));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(authenticationSettings, nameof(authenticationSettings));
            Requires.NotNull(logger, nameof(logger));

            Requires.NotNullOrEmpty(controlPlaneInfo.EnvironmentKeyVaultName, nameof(controlPlaneInfo.EnvironmentKeyVaultName));
            Requires.Argument(
                authenticationSettings.CertificateRefreshInterval > TimeSpan.Zero,
                nameof(authenticationSettings.CertificateRefreshInterval),
                "should be greater than zero");

            KeyVaultSecretReader = keyVaultSecretReader;
            KeyVaultName = controlPlaneInfo.EnvironmentKeyVaultName;
            RefreshInterval = authenticationSettings.CertificateRefreshInterval;
            Logger = logger;
        }

        private IKeyVaultSecretReader KeyVaultSecretReader { get; }

        private string KeyVaultName { get; }

        private TimeSpan RefreshInterval { get; }

        private IDiagnosticsLogger Logger { get; }

        /// <inheritdoc/>
        public JwtCertificateCredentialsKeyVaultCache New(string certName, bool startPeriodicRefreshes = true, string keyVaultName = null)
        {
            Requires.NotNullOrEmpty(certName, nameof(certName));

            var cache = new JwtCertificateCredentialsKeyVaultCache(
                KeyVaultSecretReader,
                string.IsNullOrEmpty(keyVaultName) ? KeyVaultName : keyVaultName,
                certName,
                Logger);

            if (startPeriodicRefreshes)
            {
                cache.StartPeriodicRefresh(RefreshInterval);
            }

            return cache;
        }
    }
}
