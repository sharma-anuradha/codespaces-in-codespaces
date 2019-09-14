// <copyright file="VirtualMachineTokenValidator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.Common.Crypto.Utilities;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Validates virtual machine token.
    /// </summary>
    public class VirtualMachineTokenValidator : IVirtualMachineTokenValidator
    {
        private readonly string issuer;
        private readonly string audience;
        private readonly AsyncLazy<JwtTokenGenerator> jwtTokenValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineTokenValidator"/> class.
        /// </summary>
        /// <param name="certificateProvider">Certificate provider.</param>
        /// <param name="certificateSettings">Certificate settings.</param>
        /// <param name="logger">logger.</param>
        public VirtualMachineTokenValidator(ICertificateProvider certificateProvider, CertificateSettings certificateSettings, IDiagnosticsLogger logger)
        {
            Requires.NotNull(certificateProvider, nameof(certificateProvider));
            Requires.NotNullOrWhiteSpace(certificateSettings.Issuer, nameof(certificateSettings.Issuer));
            Requires.NotNullOrWhiteSpace(certificateSettings.Audience, nameof(certificateSettings.Audience));

            issuer = certificateSettings.Issuer;
            audience = certificateSettings.Audience;
            jwtTokenValidator = new AsyncLazy<JwtTokenGenerator>(async () => new JwtTokenGenerator(await certificateProvider.GetValidCertificatesAsync(logger), logger));
        }

        /// <inheritdoc/>
        public async Task<JwtPayload> GetPayload(string token)
        {
            var tokenGenerator = await GetJwtTokenGenerator();
            return tokenGenerator.GetJwtPayload(token, issuer, audience);
        }

        /// <inheritdoc/>
        public async Task<TokenValidationParameters> GetTokenValidationParameters()
        {
            var tokenGenerator = await GetJwtTokenGenerator();
            return tokenGenerator.GetTokenValidationParameters(issuer, audience);
        }

        private async Task<JwtTokenGenerator> GetJwtTokenGenerator()
        {
            var tokenValidator = await jwtTokenValidator.Value;
            return tokenValidator;
        }
    }
}
