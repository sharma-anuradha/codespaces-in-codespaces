﻿// <copyright file="FPAAccessTokenHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http
{
    /// <summary>
    /// A handler that adds the FPA Authorization header.
    /// </summary>
    public class FPAAccessTokenHandler : DelegatingHandler
    {
        private const string AuthTokenPrefix = "bearer ";

        /// <summary>
        /// Initializes a new instance of the <see cref="FPAAccessTokenHandler"/> class.
        /// </summary>
        /// <param name="innerHandler">The inner handler.</param>
        /// <param name="keyVaultSecretReader">the keyvault secret reader.</param>
        /// <param name="controlPlaneInfo">The control plane.</param>
        /// <param name="firstPartyAppSettings">The first party app settings.</param>
        /// <param name="logger">the logger.</param>
        public FPAAccessTokenHandler(
            HttpMessageHandler innerHandler,
            IKeyVaultSecretReader keyVaultSecretReader,
            IControlPlaneInfo controlPlaneInfo,
            FirstPartyAppSettings firstPartyAppSettings,
            IDiagnosticsLogger logger)
            : base(innerHandler)
        {
            KeyVaultSecretReader = Requires.NotNull(keyVaultSecretReader, nameof(keyVaultSecretReader));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            FirstPartyAppSettings = Requires.NotNull(firstPartyAppSettings, nameof(firstPartyAppSettings));
            Logger = logger;
        }

        private IKeyVaultSecretReader KeyVaultSecretReader { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private FirstPartyAppSettings FirstPartyAppSettings { get; }

        private IDiagnosticsLogger Logger { get; }

        private IConfidentialClientApplication ConfidentialClientApplication { get; set; }

        private DateTime LastCertificateUpdate { get; set; }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var authToken = await GetFPAToken();

            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetFPAToken()
        {
            // TODO: Unify this code with other FPA accessor.
            // re-get the cert every 4 hours to ensure we're using a current value.
            if (DateTime.UtcNow > LastCertificateUpdate.AddHours(4))
            {
                LastCertificateUpdate = DateTime.UtcNow;
                var certs = await KeyVaultSecretReader.GetValidCertificatesAsync(
                    AuthenticationConstants.DefaultVisualStudioServicesApiAppIdKeyVaultName,
                    AuthenticationConstants.DefaultVisualStudioServicesApiAppIdKeyVaultCertificateName,
                    Logger);
                var cert = certs.OrderByDescending((cert) => cert.ExpiresAt).FirstOrDefault();

                ConfidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(AuthenticationConstants.VisualStudioServicesApiAppId)
                .WithAuthority(FirstPartyAppSettings.Authority, FirstPartyAppSettings.AuthorityTenantId, true)
                .WithCertificate(new System.Security.Cryptography.X509Certificates.X509Certificate2(cert.RawBytes))
                .Build();
            }

            var token = await ConfidentialClientApplication
                .AcquireTokenForClient(new[] { FirstPartyAppSettings.Scope })
                .WithAuthority(FirstPartyAppSettings.Authority, FirstPartyAppSettings.AuthorityTenantId, true)
                .WithSendX5C(true)
                .ExecuteAsync();

            return token.AccessToken;
        }
    }
}
