// <copyright file="FirstPartyTokenBuilder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A helper to build first party AAD access tokens.
    /// </summary>
    public class FirstPartyTokenBuilder : IFirstPartyTokenBuilder
    {
        private readonly IFirstPartyCertificateReader certificateReader;
        private readonly FirstPartyAppSettings firstPartyAppSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstPartyTokenBuilder"/> class.
        /// </summary>
        /// <param name="certificateReader">A first party certificate reader.</param>
        /// <param name="firstPartyAppSettings">The first party app settings.</param>
        public FirstPartyTokenBuilder(
            IFirstPartyCertificateReader certificateReader,
            FirstPartyAppSettings firstPartyAppSettings)
        {
            this.certificateReader = certificateReader;
            this.firstPartyAppSettings = firstPartyAppSettings;
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> GetFpaTokenAsync(IDiagnosticsLogger logger)
        {
            // the certificate reader caches the certificate for 4 hours
            var certificate = await certificateReader.GetApiFirstPartyAppCertificate(logger);

            var builder = PrepareApplicationBuilder(
                firstPartyAppSettings.ApiFirstPartyAppId,
                firstPartyAppSettings.AuthorityTenantId,
                new X509Certificate2(certificate.RawBytes));

            var token = await builder
                .AcquireTokenForClient(new[] { firstPartyAppSettings.Scope })
                .WithAuthority(firstPartyAppSettings.Authority, firstPartyAppSettings.AuthorityTenantId, true)
                .WithSendX5C(true)
                .ExecuteAsync();

            return token;
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> GetFpaTokenAsync(string tenantId, IDiagnosticsLogger logger)
        {
            // the certificate reader caches the certificate for 4 hours
            var certificate = await certificateReader.GetApiFirstPartyAppCertificate(logger);

            var builder = PrepareApplicationBuilder(
                firstPartyAppSettings.ApiFirstPartyAppId,
                tenantId,
                new X509Certificate2(certificate.RawBytes));

            var token = await builder
                .AcquireTokenForClient(new[] { firstPartyAppSettings.Scope })
                .WithAuthority(firstPartyAppSettings.Authority, tenantId, true)
                .WithSendX5C(true)
                .ExecuteAsync();

            return token;
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> GetMsiResourceTokenAsync(string authorityUri, string resource, IDiagnosticsLogger logger)
        {
            // the certificate reader caches the certificate for 4 hours
            var certificate = await certificateReader.GetMsiFirstPartyAppCertificate(logger);

            IConfidentialClientApplication builder = PrepareApplicationBuilder(
                firstPartyAppSettings.MsiFirstPartyAppId,
                firstPartyAppSettings.AuthorityTenantId,
                new X509Certificate2(certificate.RawBytes));

            // The scope to request for a client credential flow is the name of the resource followed by /.default.
            // This notation tells Azure AD to use the application level permissions declared statically during the application registration.
            var token = await builder
                .AcquireTokenForClient(new[] { $"{resource}.default" })
                .WithAuthority(authorityUri, true)
                .WithSendX5C(true)
                .ExecuteAsync();

            return token;
        }

        private IConfidentialClientApplication PrepareApplicationBuilder(string clientId, string tenantId, X509Certificate2 certificate)
        {
            return ConfidentialClientApplicationBuilder
                 .Create(clientId)
                 .WithAuthority(firstPartyAppSettings.Authority, tenantId, true)
                 .WithCertificate(certificate)
                 .Build();
        }
    }
}
