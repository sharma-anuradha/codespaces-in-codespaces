// <copyright file="FirstPartyCertificateReader.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Diagnostics;

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
        /// <param name="certificateKeyVaultSecretCache">certificate cache.</param>
        /// <param name="firstPartyAppSettings"> first party app settings.</param>
        public FirstPartyCertificateReader(
            ICertificateKeyVaultSecretCache certificateKeyVaultSecretCache,
            FirstPartyAppSettings firstPartyAppSettings)
        {
            CertificateKeyVaultSecretCache = Requires.NotNull(certificateKeyVaultSecretCache, nameof(certificateKeyVaultSecretCache));
            FirstPartyAppSettings = Requires.NotNull(firstPartyAppSettings, nameof(firstPartyAppSettings));
            CertificatePostFix = CommonUtils.NotNullOrWhiteSpace(FirstPartyAppSettings.DomainName, nameof(FirstPartyAppSettings.DomainName), nameof(FirstPartyAppSettings)).Replace(".", "-");
        }

        private ICertificateKeyVaultSecretCache CertificateKeyVaultSecretCache { get; }

        private FirstPartyAppSettings FirstPartyAppSettings { get; }

        private string CertificatePostFix { get; }

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
            return await GetCertificate(ApiKeyVaultCertificateName, logger);
        }

        /// <inheritdoc/>
        public async Task<Certificate> GetMsiFirstPartyAppCertificate(IDiagnosticsLogger logger)
        {
            return await GetCertificate(MsiKeyVaultCertificateName, logger);
        }

        private async Task<Certificate> GetCertificate(string certificateName, IDiagnosticsLogger logger)
        {
            return await CertificateKeyVaultSecretCache.GetSecretAsync(certificateName, logger);
        }
    }
}