// <copyright file="TokenHandlingWarmup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.TokenService.Settings;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.TokenService
{
    /// <summary>
    /// Configures token reader/writer settings after services are available.
    /// </summary>
    internal class TokenHandlingWarmup : IAsyncWarmup
    {
        private static readonly TimeSpan CertificateRefreshPeriod = TimeSpan.FromDays(1);
        private readonly List<IAsyncWarmup> warmups;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenHandlingWarmup"/> class.
        /// </summary>
        /// <param name="appSettings">App settings (from services)..</param>
        /// <param name="tokenReader">Token reader (from services).</param>
        /// <param name="tokenWriter">Token writer (from services).</param>
        /// <param name="keyVaultReader">Key vault reader (from services).</param>
        /// <param name="loggerFactory">Logger factory (from services).</param>
        public TokenHandlingWarmup(
            AppSettings appSettings,
            IJwtReader tokenReader,
            IJwtWriter tokenWriter,
            IKeyVaultSecretReader keyVaultReader,
            IDiagnosticsLoggerFactory loggerFactory)
        {
            TokenServiceAppSettings tokenServiceSettings = appSettings.TokenService;
            ControlPlaneSettings controlPlaneSettings = appSettings.ControlPlaneSettings;

            var vaultName = string.Join(
                '-',
                controlPlaneSettings.Prefix,
                "online",
                controlPlaneSettings.EnvironmentName,
                "kv");

            var logger = loggerFactory.New();

            this.warmups = new List<IAsyncWarmup>();

            // Configure issuers from settings.
            foreach (var issuerSettings in tokenServiceSettings?.IssuerSettings
                ?? Enumerable.Empty<KeyValuePair<string, TokenIssuerSettings>>())
            {
                var signingCredentials = new JwtCertificateCredentialsKeyVaultCache(
                    keyVaultReader,
                    vaultName,
                    issuerSettings.Value.SigningCertificateName,
                    logger);
                signingCredentials.StartPeriodicRefresh(CertificateRefreshPeriod);
                this.warmups.Add(signingCredentials);

                tokenReader.AddIssuer(
                    issuerSettings.Value.IssuerUri,
                    signingCredentials.ConvertToPublic());
                if (!issuerSettings.Value.ValidateOnly)
                {
                    tokenWriter.AddIssuer(
                        issuerSettings.Value.IssuerUri,
                        signingCredentials);
                }

                logger.AddValue("Issuer", issuerSettings.Value.IssuerUri);
                logger.AddValue("VaultName", vaultName);
                logger.AddValue("CertificateName", issuerSettings.Value.SigningCertificateName);
                logger.LogInfo("token_service_issuer_config");
            }

            // Configure audiences from settings.
            foreach (var audienceSettings in tokenServiceSettings?.AudienceSettings
                ?? Enumerable.Empty<KeyValuePair<string, TokenAudienceSettings>>())
            {
                var encryptingCertificateName = audienceSettings.Value.EncryptingCertificateName;
                if (string.IsNullOrEmpty(encryptingCertificateName))
                {
                    // Tokens for this audience are not encrypted.
                    tokenWriter.AddAudience(audienceSettings.Value.AudienceUri);
                    if (!audienceSettings.Value.IssueOnly)
                    {
                        tokenReader.AddAudience(audienceSettings.Value.AudienceUri);
                    }

                    logger.AddValue("VaultName", vaultName);
                    logger.AddValue("CertificateName", encryptingCertificateName);
                }
                else
                {
                    // Cache encrypting credentials for this audience.
                    var encryptingCredentials = new JwtCertificateCredentialsKeyVaultCache(
                            keyVaultReader,
                            vaultName,
                            encryptingCertificateName,
                            logger);
                    encryptingCredentials.StartPeriodicRefresh(CertificateRefreshPeriod);
                    this.warmups.Add(encryptingCredentials);

                    tokenWriter.AddAudience(
                        audienceSettings.Value.AudienceUri,
                        encryptingCredentials.ConvertToPublic());
                    if (!audienceSettings.Value.IssueOnly)
                    {
                        tokenReader.AddAudience(
                            audienceSettings.Value.AudienceUri,
                            encryptingCredentials!);
                    }
                }

                logger.AddValue("Audience", audienceSettings.Value.AudienceUri);
                logger.LogInfo("token_service_audience_config");
            }
        }

        /// <inheritdoc/>
        public Task WarmupCompletedAsync()
        {
            // Wait for all the certificate credentials to be loaded from keyvaults.
            return Task.WhenAll(this.warmups.Select((w) => w.WarmupCompletedAsync()));
        }
    }
}
