// <copyright file="VirtualMachineTokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.Common.Crypto.Utilities;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Virtual machine token provider.
    /// </summary>
    public class VirtualMachineTokenProvider : IVirtualMachineTokenProvider
    {
        /// <summary>
        /// Days till the token is valid.
        /// </summary>
        private const int DaysTillExpiry = 365;
        private readonly TimeSpan expiresAfter = TimeSpan.FromDays(DaysTillExpiry);

        private readonly string issuer;
        private readonly string audience;
        private readonly AsyncLazy<JwtTokenGenerator> jwtTokenGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineTokenProvider"/> class.
        /// </summary>
        /// <param name="certificateProvider">Certificate provider.</param>
        /// <param name="certificateSettings">Certificate settings.</param>
        /// <param name="logger">logger.</param>
        public VirtualMachineTokenProvider(ICertificateProvider certificateProvider, CertificateSettings certificateSettings, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrWhiteSpace(certificateSettings.Issuer, nameof(certificateSettings.Issuer));
            Requires.NotNullOrWhiteSpace(certificateSettings.Audience, nameof(certificateSettings.Audience));
            Requires.NotNull(logger, nameof(logger));

            this.issuer = certificateSettings.Issuer;
            this.audience = certificateSettings.Audience;
            this.jwtTokenGenerator = new AsyncLazy<JwtTokenGenerator>(async () => new JwtTokenGenerator(await certificateProvider.GetValidCertificatesAsync(logger), logger));
        }

        /// <inheritdoc/>
        public async Task<string> GenerateAsync(string identifier, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrWhiteSpace(identifier, nameof(identifier));
            Requires.NotNull(logger, nameof(logger));

            var tokenGenerator = await this.jwtTokenGenerator.Value;
            var expiresAt = DateTime.UtcNow.Add(expiresAfter);
            var payload = tokenGenerator.NewJwtPayload(issuer, audience, identifier, null, expiresAt, logger);
            return tokenGenerator.WriteToken(payload);
        }
    }
}
