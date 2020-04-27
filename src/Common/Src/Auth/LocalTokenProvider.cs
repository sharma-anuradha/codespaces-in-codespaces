// <copyright file="LocalTokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Token provider.
    /// </summary>
    public class LocalTokenProvider : ITokenProvider
    {
        private readonly IJwtWriter jwtWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalTokenProvider"/> class.
        /// </summary>
        /// <param name="jwtWriter">JWT writer.</param>
        /// <param name="authenticationSettings">Authentication settings.</param>
        public LocalTokenProvider(
            IJwtWriter jwtWriter,
            AuthenticationSettings authenticationSettings)
        {
            this.jwtWriter = Requires.NotNull(jwtWriter, nameof(jwtWriter));
            Settings = Requires.NotNull(authenticationSettings, nameof(authenticationSettings));

            ValidateTokenSettings(authenticationSettings.VmTokenSettings, nameof(authenticationSettings.VmTokenSettings));
            ValidateTokenSettings(authenticationSettings.VsSaaSTokenSettings, nameof(authenticationSettings.VsSaaSTokenSettings));
            ValidateTokenSettings(authenticationSettings.ConnectionTokenSettings, nameof(authenticationSettings.ConnectionTokenSettings));
        }

        /// <inheritdoc/>
        public AuthenticationSettings Settings { get; }

        /// <inheritdoc/>
        public Task<string> IssueTokenAsync(
            string issuer,
            string audience,
            DateTime expires,
            IEnumerable<Claim> claims,
            IDiagnosticsLogger logger)
        {
            string token = this.jwtWriter.WriteToken(
                logger, issuer, audience, expires, claims.ToArray());
            return Task.FromResult(token);
        }

        private static void ValidateTokenSettings(TokenSettings tokenSettings, string fieldName)
        {
            Requires.NotNull(tokenSettings, fieldName);
            Requires.NotNullOrWhiteSpace(tokenSettings.Issuer, $"{fieldName}.{nameof(tokenSettings.Issuer)}");
            Requires.NotNullOrWhiteSpace(tokenSettings.Audience, $"{fieldName}.{nameof(tokenSettings.Audience)}");
        }
    }
}
