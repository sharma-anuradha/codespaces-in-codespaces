// <copyright file="TokenServiceCertificateCredentialsCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.TokenService.Client
{
    /// <summary>
    /// Caches public certificates retrieved from the token service for an issuer, so that
    /// tokens signed with the issuer certificate can be validated.
    /// </summary>
    public class TokenServiceCertificateCredentialsCache : JwtCertificateCredentialsCache
    {
        private readonly TokenServiceClient tokenServiceClient;
        private readonly string issuer;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="TokenServiceCertificateCredentialsCache"/> class.
        /// </summary>
        /// <param name="tokenServiceClient">Client for the token service.</param>
        /// <param name="issuer">The issuer that public certificates are requested for.</param>
        /// <param name="logger">Diagnostic logger.</param>
        public TokenServiceCertificateCredentialsCache(
            TokenServiceClient tokenServiceClient,
            string issuer,
            IDiagnosticsLogger logger)
            : base(logger)
        {
            this.tokenServiceClient =
                Requires.NotNull(tokenServiceClient, nameof(tokenServiceClient));
            Requires.NotNullOrEmpty(issuer, nameof(issuer));
            this.issuer = issuer;
        }

        /// <summary>
        /// Loads certificates from the token service.
        /// </summary>
        /// <returns>The loaded certificates, or null if the issuer is not found.</returns>
        /// <remarks>
        /// Any exceptions thrown by this method are caught and logged by the caller.
        /// </remarks>
        protected override async Task<IEnumerable<X509Certificate2>?> LoadCertificatesAsync()
        {
            return await this.tokenServiceClient.GetIssuerPublicCertificatesAsync(
                this.issuer, CancellationToken.None);
        }
    }
}
