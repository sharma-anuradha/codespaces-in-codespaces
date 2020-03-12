// <copyright file="TokenProviderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Extensions adding specific token functionality to <see cref="ITokenProvider"/>.
    /// </summary>
    public static class TokenProviderExtensions
    {
        private static readonly TimeSpan DefaultVmTokenLifetime = TimeSpan.FromDays(365);

        /// <summary>
        /// Generates a VM token.
        /// </summary>
        /// <param name="provider">The token provider.</param>
        /// <param name="identifier">Id of the resource.</param>
        /// <param name="logger">logger.</param>
        /// <returns>security token.</returns>
        public static Task<string> GenerateVmTokenAsync(
            this ITokenProvider provider,
            string identifier,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrWhiteSpace(identifier, nameof(identifier));
            Requires.NotNull(logger, nameof(logger));

            var expiresAt = DateTime.UtcNow.Add(
                provider.Settings.VmTokenSettings.Lifetime ?? DefaultVmTokenLifetime);

            logger.AddValue("jwt_sub", identifier);

            var token = provider.JwtWriter.WriteToken(
                issuer: provider.Settings.VmTokenSettings.Issuer,
                audience: provider.Settings.VmTokenSettings.Audience,
                expires: expiresAt,
                subject: identifier,
                logger: logger);

            return Task.FromResult(token);
        }
    }
}
