﻿// <copyright file="TokenProviderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
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
        public static string GenerateEnvironmentConnectionToken(
            this ITokenProvider provider,
            CloudEnvironment environment,
            ICloudEnvironmentSku environmentSku,
            Profile userProfile,
            IDiagnosticsLogger logger)
        {
            var requiredClaims = new[]
            {
                // Copy the user identity claims. The user may have authenticated using a
                // different scheme, but they must have a profile at this point. This normalizes
                // the tokens used for environment connection.
                new Claim(CustomClaims.Provider, userProfile.Provider),
                new Claim(CustomClaims.UserId, userProfile.Id),
            };

            var settings = provider.Settings.ConnectionTokenSettings;
            var lifetime = settings.Lifetime ?? DefaultConnectionTokenLifetime;
            var payload = new JwtPayload(
                issuer: settings.Issuer,
                audience: settings.Audience,
                requiredClaims,
                notBefore: null,
                expires: DateTime.UtcNow + lifetime);

            if (environmentSku.ComputeOS == ComputeOS.Linux)
            {
                // For Linux environments, make the token scoped to this one sharing session.
                // Credential helpers in the environment also need access to join the session.
                // TODO: Enable scoped tokens for Windows (Nexus) environments also.
                payload.AddClaim(new Claim(CustomClaims.Scope, string.Join(
                    ' ',
                    PlanAccessTokenScopes.ShareSession,
                    PlanAccessTokenScopes.JoinSession)));
                payload.AddClaim(
                    new Claim(CustomClaims.Session, environment.Connection.ConnectionSessionId));
            }

            // Copy additional optional identity claims from the authenticated user.
            AddOptionalClaim(payload, CustomClaims.DisplayName, userProfile.Name);
            AddOptionalClaim(payload, CustomClaims.Email, userProfile.Email);
            AddOptionalClaim(payload, CustomClaims.Username, userProfile.UserName);

            var token = provider.JwtWriter.WriteToken(payload, logger);
            return token;
        }

        private static void AddOptionalClaim(JwtPayload payload, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                payload.AddClaim(new Claim(name, value));
            }
        }
    }
}
