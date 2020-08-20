// <copyright file="TokenProviderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions
{
    /// <summary>
    /// Extensions adding specific token functionality to <see cref="ITokenProvider"/>.
    /// </summary>
    public static class TokenProviderExtensions
    {
        private static readonly TimeSpan DefaultConnectionTokenLifetime = TimeSpan.FromDays(30);

        /// <summary>
        /// Generates a token that enables an environment to accept connections.
        /// </summary>
        /// <param name="provider">The token provider.</param>
        /// <param name="environment">Environment that will connect with the token.</param>
        /// <param name="environmentSku">SKU of the environment.</param>
        /// <param name="userProfile">Profile of the environment owner.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>JWT.</returns>
        public static async Task<string> GenerateEnvironmentConnectionTokenAsync(
            this ITokenProvider provider,
            CloudEnvironment environment,
            ICloudEnvironmentSku environmentSku,
            Profile userProfile,
            IDiagnosticsLogger logger)
        {
            var claims = new List<Claim>
            {
                // Copy the user identity claims. The user may have authenticated using a
                // different scheme, but they must have a profile at this point. This normalizes
                // the tokens used for environment connection.
                new Claim(CustomClaims.Provider, userProfile.Provider),
                new Claim(CustomClaims.UserId, userProfile.Id),
            };

            var settings = provider.Settings.ConnectionTokenSettings;
            var lifetime = settings.Lifetime ?? DefaultConnectionTokenLifetime;

            if (environmentSku.ComputeOS == ComputeOS.Linux)
            {
                // For Linux environments, make the token scoped to this one sharing session.
                // Credential helpers in the environment also need access to join the session.
                // TODO: Enable scoped tokens for Windows (Nexus) environments also.
                claims.Add(new Claim(CustomClaims.Scope, string.Join(
                    ' ',
                    PlanAccessTokenScopes.ShareSession,
                    PlanAccessTokenScopes.JoinSession)));
                if (environment.State != CloudEnvironmentState.Exporting)
                {
                    claims.Add(
                        new Claim(CustomClaims.Session, environment.Connection.WorkspaceId));
                }
            }

            // Copy additional optional identity claims from the authenticated user.
            AddOptionalClaim(claims, CustomClaims.DisplayName, userProfile.Name);
            AddOptionalClaim(claims, CustomClaims.Email, userProfile.Email);
            AddOptionalClaim(claims, CustomClaims.Username, userProfile.UserName);

            var token = await provider.IssueTokenAsync(
                settings.Issuer,
                settings.Audience,
                DateTime.UtcNow + lifetime,
                claims,
                logger);
            return token;
        }

        private static void AddOptionalClaim(IList<Claim> claims, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                claims.Add(new Claim(name, value));
            }
        }
    }
}
