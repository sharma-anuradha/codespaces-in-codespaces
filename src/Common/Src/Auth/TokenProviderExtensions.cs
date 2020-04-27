// <copyright file="TokenProviderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

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
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>security token.</returns>
        public static async Task<string> GenerateVmTokenAsync(
            this ITokenProvider provider,
            string identifier,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrWhiteSpace(identifier, nameof(identifier));

            var expiresAt = DateTime.UtcNow.Add(
                provider.Settings.VmTokenSettings.Lifetime ?? DefaultVmTokenLifetime);

            var token = await provider.IssueTokenAsync(
                provider.Settings.VmTokenSettings.Issuer,
                provider.Settings.VmTokenSettings.Audience,
                expiresAt,
                new[] { new Claim(JwtRegisteredClaimNames.Sub, identifier) },
                logger);
            return token;
        }
    }
}
