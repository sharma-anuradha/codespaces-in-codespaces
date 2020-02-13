// <copyright file="TokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Token provider.
    /// </summary>
    public class TokenProvider : ITokenProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TokenProvider"/> class.
        /// </summary>
        /// <param name="jwtWriter">JWT writer.</param>
        /// <param name="authenticationSettings">Authentication settings.</param>
        public TokenProvider(IJwtWriter jwtWriter, AuthenticationSettings authenticationSettings)
        {
            Requires.NotNull(jwtWriter, nameof(jwtWriter));
            Requires.NotNull(authenticationSettings, nameof(authenticationSettings));

            ValidateTokenSettings(authenticationSettings.VmTokenSettings, nameof(authenticationSettings.VmTokenSettings));
            ValidateTokenSettings(authenticationSettings.VsSaaSTokenSettings, nameof(authenticationSettings.VsSaaSTokenSettings));
            ValidateTokenSettings(authenticationSettings.ConnectionTokenSettings, nameof(authenticationSettings.ConnectionTokenSettings));

            this.Settings = authenticationSettings;
            this.JwtWriter = jwtWriter;
        }

        /// <inheritdoc/>
        public AuthenticationSettings Settings { get; }

        /// <inheritdoc/>
        public IJwtWriter JwtWriter { get; }

        private static void ValidateTokenSettings(TokenSettings tokenSettings, string fieldName)
        {
            Requires.NotNull(tokenSettings, fieldName);
            Requires.NotNullOrWhiteSpace(tokenSettings.Issuer, $"{fieldName}.{nameof(tokenSettings.Issuer)}");
            Requires.NotNullOrWhiteSpace(tokenSettings.Audience, $"{fieldName}.{nameof(tokenSettings.Audience)}");
        }
    }
}
