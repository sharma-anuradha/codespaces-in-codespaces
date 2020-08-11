// <copyright file="CertificateKeyVaultSecretCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Certificate key vault secret cache.
    /// </summary>
    public class CertificateKeyVaultSecretCache : BaseKeyVaultSecretCache<Certificate>, ICertificateKeyVaultSecretCache
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateKeyVaultSecretCache"/> class.
        /// </summary>
        /// <param name="keyVaultSecretReader">keyVault reader.</param>
        /// <param name="controlPlaneInfo">control plane info.</param>
        public CertificateKeyVaultSecretCache(
            IKeyVaultSecretReader keyVaultSecretReader,
            IControlPlaneInfo controlPlaneInfo)
            : base(keyVaultSecretReader, controlPlaneInfo)
        {
        }

        /// <inheritdoc/>
        protected override async Task<Certificate> RefreshSecretAsync(string key, IDiagnosticsLogger logger)
        {
            var certificate = (Certificate)default;

            await logger.OperationScopeAsync(
               $"{LogBaseName}_refresh_certificate",
               async (childLogger) =>
               {
                   childLogger.AddBaseValue("CertificateKey", key);
                   var certs = await KeyVaultSecretReader.GetValidCertificatesAsync(
                        ControlPlaneInfo.EnvironmentKeyVaultName,
                        key,
                        logger);
                   var cert = certs.OrderByDescending((cert) => cert.ExpiresAt).FirstOrDefault();
                   Cache[key] = cert;
                   certificate = (Certificate)cert;
               },
               (ex, childLogger) =>
               {
                   childLogger.FluentAddValue("ErrorMessage", ex.Message)
                               .FluentAddValue("ErrorSource", ex.Source)
                               .FluentAddValue("Exception", ex.ToString())
                               .LogException("certificate_refresh_error", ex);
                   return Task.FromResult((Certificate)default);
               }, swallowException: true);

            return certificate;
        }
    }
}
