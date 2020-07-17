// <copyright file="FPAAccessTokenHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

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
        /// <param name="firstPartyAppSettings">The first party app settings.</param>
        /// <param name="firstPartyCertReader">The first party certificate reader.</param>
        /// <param name="logger">the logger.</param>
        public FPAAccessTokenHandler(
            HttpMessageHandler innerHandler,
            FirstPartyAppSettings firstPartyAppSettings,
            IFirstPartyCertificateReader firstPartyCertReader,
            IDiagnosticsLogger logger)
            : base(innerHandler)
        {
            FirstPartyAppSettings = Requires.NotNull(firstPartyAppSettings, nameof(firstPartyAppSettings));
            FirstPartyCertificateReader = Requires.NotNull(firstPartyCertReader, nameof(firstPartyCertReader));
            Logger = logger;
        }

        private FirstPartyAppSettings FirstPartyAppSettings { get; }

        private IFirstPartyCertificateReader FirstPartyCertificateReader { get; }

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
                var cert = await FirstPartyCertificateReader.GetApiFirstPartyAppCertificate(Logger);

                ConfidentialClientApplication = ConfidentialClientApplicationBuilder
                 .Create(FirstPartyAppSettings.ApiFirstPartyAppId)
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
