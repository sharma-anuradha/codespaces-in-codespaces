// <copyright file="VMTokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.Common.Crypto.Utilities;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Creates a Jwt token provider tailored for VM resource ids.
    /// </summary>
    public class VMTokenProvider : IVSSaaSTokenProvider
    {
        /// <summary>
        /// Days till the token is valid.
        /// Approximately 2x the validity of the certificate.
        /// </summary>
        private const int DaysTillExpiry = 600;

        private readonly TimeSpan expiresAfter = TimeSpan.FromDays(DaysTillExpiry);
        private readonly IDiagnosticsLogger logger;
        private JwtTokenGenerator jwtTokenGenerator;
        private string issuer;
        private string audience;

        /// <summary>
        /// Initializes a new instance of the <see cref="VMTokenProvider"/> class.
        /// </summary>
        /// <param name="azureClientFactory">Azure client factory.</param>
        /// <param name="servicePrincipal">The application service principal.</param>
        /// <param name="controlPlaneInfo">The control-plane info.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Control plane resource accessor.</param>
        /// <param name="certificateSettings">Certificate settings.</param>
        /// <param name="logger">Logger.</param>
        public VMTokenProvider(
            IAzureClientFactory azureClientFactory,
            IServicePrincipal servicePrincipal,
            IControlPlaneInfo controlPlaneInfo,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            CertificateSettings certificateSettings,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureClientFactory, nameof(azureClientFactory));
            Requires.NotNull(servicePrincipal, nameof(servicePrincipal));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
            Requires.NotNull(certificateSettings, nameof(certificateSettings));
            this.logger = Requires.NotNull(logger, nameof(logger));

            Requires.NotNullOrWhiteSpace(certificateSettings.CertificateName, nameof(certificateSettings.CertificateName));
            Requires.NotNullOrWhiteSpace(certificateSettings.Issuer, nameof(certificateSettings.Issuer));
            Requires.NotNullOrWhiteSpace(certificateSettings.Audience, nameof(certificateSettings.Audience));

            Task.Run(() => Init(azureClientFactory, servicePrincipal, controlPlaneInfo, controlPlaneAzureResourceAccessor, certificateSettings, logger)).Wait();
        }

        /// <summary>
        /// Generates a Jwt token given the machine identifier.
        /// </summary>
        /// <param name="identifier">Id of the VM resource.</param>
        /// <returns>Jwt VMToken.</returns>
        public string Generate(string identifier)
        {
            DateTime expiresAt = DateTime.UtcNow.Add(expiresAfter);
            var payload = this.jwtTokenGenerator.NewJwtPayload(issuer, audience, identifier, null, expiresAt);
            return this.jwtTokenGenerator.WriteToken(payload);
        }

        private static async Task<byte[]> GetRawCertificateBytesAsync(IVault vault, string certificateId)
        {
            var secret = await vault.Client.GetSecretAsync(certificateId);
            return Convert.FromBase64String(secret.Value);
        }

        private async Task Init(
            IAzureClientFactory azureClientFactory,
            IServicePrincipal servicePrincipal,
            IControlPlaneInfo controlPlaneInfo,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            CertificateSettings certificateSettings,
            IDiagnosticsLogger logger)
        {
            var subscriptionId = await controlPlaneAzureResourceAccessor.GetCurrentSubscriptionIdAsync();
            var clientSecret = await servicePrincipal.GetServicePrincipalClientSecretAsync();

            IAzure client = await azureClientFactory.GetAzureClientAsync(
                subscriptionId,
                servicePrincipal.ClientId,
                clientSecret,
                servicePrincipal.TenantId);

            var keyVaultName = controlPlaneInfo.EnvironmentKeyVaultName;
            var keyVaultResourceGroup = controlPlaneInfo.EnvironmentResourceGroupName;
            var keyVaultUrl = $"https://{keyVaultName}.vault.azure.net";
            var keyVault = await client.Vaults.GetByResourceGroupAsync(keyVaultResourceGroup, keyVaultName);

            var (primarySecretVersion, secondarySecretsVersion) = await GetPrimaryAndSecondarySecretsAsync(keyVault, keyVaultUrl, certificateSettings.CertificateName);

            var primaryCertificate = await GetRawCertificateBytesAsync(keyVault, primarySecretVersion);
            var secondaryCertificate = await GetRawCertificateBytesAsync(keyVault, secondarySecretsVersion.FirstOrDefault()); // TODO: janraj, modify JwtTokenGenerator to take more than one secondary

            this.jwtTokenGenerator = new JwtTokenGenerator(primaryCertificate, secondaryCertificate, logger);
            this.issuer = certificateSettings.Issuer;
            this.audience = certificateSettings.Audience;
        }

        private async Task<(string, string[])> GetPrimaryAndSecondarySecretsAsync(IVault keyVault, string keyVaultUrl, string certificateName)
        {
            var secretItems = await keyVault.Client.GetSecretVersionsAsync(keyVaultUrl, certificateName);

            List<SecretItem> enabledSecrets = new List<SecretItem>();
            foreach (var item in secretItems)
            {
                if (item.Attributes.Enabled == true && item.Attributes.NotBefore.HasValue && item.Attributes.NotBefore.Value < DateTime.UtcNow)
                {
                    enabledSecrets.Add(item);
                }
            }

            string primarySecretVersion = default;
            string[] secondarySecretVersions = default;

            try
            {
                var orderedSecretVersions = enabledSecrets.Where(x => x.Attributes.Created.HasValue).OrderByDescending(x => x.Attributes.Created);
                primarySecretVersion = orderedSecretVersions.First().Id;
                secondarySecretVersions = orderedSecretVersions
                    .Skip(1)
                    .Where(x => x.Attributes.Expires.HasValue && x.Attributes.Expires.Value.AddYears(1) > DateTime.UtcNow)
                    .Select(x => x.Id)
                    .ToArray();
            }
            catch (Exception e)
            {
                this.logger
                    .WithValue("Exception", e.ToString())
                    .LogError("failed_to_get_primary_and_secondary_secret_versions");
                throw;
            }

            this.logger.LogInfo("primary_and_secondary_secret_versions_found");
            return (primarySecretVersion, secondarySecretVersions);
        }
    }
}
