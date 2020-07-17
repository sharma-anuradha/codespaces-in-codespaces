// <copyright file="FirstPartyCertificateReader.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// First Party App Info Provider.
    /// </summary>
    public class FirstPartyCertificateReader : IFirstPartyCertificateReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FirstPartyCertificateReader"/> class.
        /// </summary>
        /// <param name="keyVaultSecretReader">keyVault reader.</param>
        /// <param name="firstPartyAppSettings"> first party app settings.</param>
        /// <param name="controlPlaneInfo">control plane info.</param>
        public FirstPartyCertificateReader(
            IKeyVaultSecretReader keyVaultSecretReader,
            FirstPartyAppSettings firstPartyAppSettings,
            IControlPlaneInfo controlPlaneInfo)
        {
            KeyVaultSecretReader = Requires.NotNull(keyVaultSecretReader, nameof(keyVaultSecretReader));
            FirstPartyAppSettings = Requires.NotNull(firstPartyAppSettings, nameof(firstPartyAppSettings));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            CertificatePostFix = CommonUtils.NotNullOrWhiteSpace(FirstPartyAppSettings.DomainName, nameof(FirstPartyAppSettings.DomainName), nameof(FirstPartyAppSettings)).Replace(".", "-");
        }

        private IKeyVaultSecretReader KeyVaultSecretReader { get; }

        private FirstPartyAppSettings FirstPartyAppSettings { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private string CertificatePostFix { get; }

        private Certificate ApiFirstPartyAppCertificate { get; set; }

        private Certificate MsiFirstPartyAppCertificate { get; set; }

        private DateTime ApiLastCertificateUpdate { get; set; }

        private DateTime MsiLastCertificateUpdate { get; set; }

        /// <summary>
        /// Gets the api first party app certificate name.
        /// </summary>
        private string ApiKeyVaultCertificateName
        {
            get
            {
                return $"api-{CertificatePostFix}";
            }
        }

        /// <summary>
        /// Gets the api first party app certificate name.
        /// </summary>
        private string MsiKeyVaultCertificateName
        {
            get
            {
                return $"msi-{CertificatePostFix}";
            }
        }

        /// <inheritdoc/>
        public async Task<Certificate> GetApiFirstPartyAppCertificate(IDiagnosticsLogger logger)
        {
            return await GetCertificate(ApiKeyVaultCertificateName, ApiFirstPartyAppCertificate, ApiLastCertificateUpdate, logger);
        }

        /// <inheritdoc/>
        public async Task<Certificate> GetMsiFirstPartyAppCertificate(IDiagnosticsLogger logger)
        {
            return await GetCertificate(MsiKeyVaultCertificateName, MsiFirstPartyAppCertificate, MsiLastCertificateUpdate, logger);
        }

        private async Task<Certificate> GetCertificate(string certificateName, Certificate currentCertificate, DateTime certExpiryTracker, IDiagnosticsLogger logger)
        {
            // TODO: This logic will be updated by Auto Cert rotation feature implementation to
            // get the certificate from cache and refresh cache in background
            if (currentCertificate == default || DateTime.UtcNow > certExpiryTracker.AddHours(4))
            {
                certExpiryTracker = DateTime.UtcNow;
                var certs = await KeyVaultSecretReader.GetValidCertificatesAsync(
                    ControlPlaneInfo.EnvironmentKeyVaultName,
                    certificateName,
                    logger);
                currentCertificate = certs.OrderByDescending((cert) => cert.ExpiresAt).FirstOrDefault();
            }

            return currentCertificate;
        }
    }
}