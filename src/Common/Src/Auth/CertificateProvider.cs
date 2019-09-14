// <copyright file="CertificateProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Provides primary and secondary certificates from the control plane.
    /// </summary>
    public class CertificateProvider : ICertificateProvider
    {
        /// <summary>
        /// Grace period till we consider a certificate is valid even after it expired.
        /// This is used to get the secondary certificates which can be used for validation.
        /// </summary>
        private const int GracePeriodAfterExpiry = 365;

        private readonly TimeSpan gracePeriod = TimeSpan.FromDays(GracePeriodAfterExpiry);
        private readonly IControlPlaneAzureResourceAccessor controlPlaneAccessor;
        private readonly CertificateSettings certificateSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateProvider"/> class.
        /// </summary>
        /// <param name="controlPlaneAccessor">Control plane accessor object.</param>
        /// <param name="certificateSettings">Certificate settings.</param>
        public CertificateProvider(IControlPlaneAzureResourceAccessor controlPlaneAccessor, CertificateSettings certificateSettings)
        {
            this.controlPlaneAccessor = Requires.NotNull(controlPlaneAccessor, nameof(controlPlaneAccessor));
            this.certificateSettings = Requires.NotNull(certificateSettings, nameof(certificateSettings));
        }

        /// <inheritdoc/>
        public async Task<ValidCertificates> GetValidCertificatesAsync(IDiagnosticsLogger logger)
        {
            try
            {
                var secretItems = await controlPlaneAccessor.GetKeyVaultSecretVersionsAsync(certificateSettings.CertificateName);

                var enabledSecrets = new List<SecretItem>();
                foreach (var item in secretItems)
                {
                    if (IsEnabledAndValidSecret(item))
                    {
                        enabledSecrets.Add(item);
                    }
                }

                var primaryCertificate = await GetPrimaryCertificateAsync(enabledSecrets);
                var secondaryCertificates = await GetSecondaryCertificatesAsync(enabledSecrets, primaryCertificate.Id);

                logger.LogInfo("fetching_certificates_successful");

                return new ValidCertificates()
                {
                    Primary = primaryCertificate,
                    Secondaries = secondaryCertificates,
                };
            }
            catch (Exception e)
            {
                logger.WithValue("Exception", e.ToString()).LogError("fetching_certificates_failed");
                throw;
            }
        }

        private bool IsEnabledAndValidSecret(SecretItem secretItem)
        {
            return
                secretItem.Attributes.Enabled.HasValue &&
                secretItem.Attributes.Enabled.Value &&
                secretItem.Attributes.NotBefore.HasValue &&
                secretItem.Attributes.NotBefore.Value < DateTime.UtcNow;
        }

        private async Task<Certificate> GetPrimaryCertificateAsync(List<SecretItem> secretItems)
        {
            var orderedSecrets = secretItems.Where(s => s.Attributes.Created.HasValue).OrderByDescending(s => s.Attributes.Created.Value);
            var primaryCertificateVersion = orderedSecrets.First(s => s.Attributes.Expires.HasValue && s.Attributes.Expires.Value > DateTime.UtcNow);
            return await GetCertificateAsync(primaryCertificateVersion);
        }

        private async Task<Certificate[]> GetSecondaryCertificatesAsync(List<SecretItem> secretItems, string primaryCertificateId)
        {
            var orderedSecrets = secretItems
                .Where(s => s.Id != primaryCertificateId)
                .Where(s => s.Attributes.Expires.HasValue)
                .Where(s => s.Attributes.Expires.Value + gracePeriod > DateTime.UtcNow);

            var certificates = new List<Certificate>();

            foreach (var secretItem in orderedSecrets)
            {
                certificates.Add(await GetCertificateAsync(secretItem));
            }

            return certificates.ToArray();
        }

        private async Task<Certificate> GetCertificateAsync(SecretItem secretItem)
        {
            return new Certificate()
            {
                Id = secretItem.Id,
                RawBytes = await GetRawCertificateBytesAsync(secretItem.Identifier.Version),
                ExpiresAt = secretItem.Attributes.Expires.Value,
            };
        }

        private async Task<byte[]> GetRawCertificateBytesAsync(string certificateId)
        {
            var secret = await this.controlPlaneAccessor.GetKeyVaultSecretAsync(certificateSettings.CertificateName, certificateId);
            return Convert.FromBase64String(secret);
        }
    }
}
