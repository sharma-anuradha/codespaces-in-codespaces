// <copyright file="TokenProviderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions
{
    /// <summary>
    /// Extensions adding specific token functionality to <see cref="ITokenProvider"/>.
    /// </summary>
    public static class TokenProviderExtensions
    {
        private const int VsSaaSTokenDaysTillExpiry = 25;
        private static readonly TimeSpan VsSaaSTokenExpiration = TimeSpan.FromDays(VsSaaSTokenDaysTillExpiry);

        /// <summary>
        /// Generates a VsSaaS token.
        /// </summary>
        /// <param name="provider">The token provider.</param>
        /// <param name="plan">The plan the token applies to.</param>
        /// <param name="scopes">The scope claim of the token.</param>
        /// <param name="userClaims">The claims of the user the token is granted to.</param>
        /// <param name="requestedExpiration">The expiration of the token.</param>
        /// <param name="logger">logger.</param>
        /// <returns>security token.</returns>
        public static string GenerateVsSaaSToken(
            this ITokenProvider provider,
            VsoPlan plan,
            string[] scopes,
            IEnumerable<Claim> userClaims,
            DateTime? requestedExpiration,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(userClaims, nameof(userClaims));

            var tid =
                userClaims.FirstOrDefault((c) => c.Type == CustomClaims.TenantId)?.Value ??
                userClaims.FirstOrDefault((c) => c.Type == "http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

            var oid =
                userClaims.FirstOrDefault((c) => c.Type == CustomClaims.OId)?.Value ??
                userClaims.FirstOrDefault((c) => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            var username =
                userClaims.FirstOrDefault((c) => c.Type == CustomClaims.Username)?.Value ??
                userClaims.FirstOrDefault((c) => c.Type == CustomClaims.UniqueName)?.Value;

            var displayName = userClaims.FirstOrDefault((c) => c.Type == CustomClaims.DisplayName)?.Value;

            var expiration = userClaims.FirstOrDefault((c) => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            DateTime? sourceTokenExpiration = null;
            if (int.TryParse(expiration, out int secSinceEpoch))
            {
                sourceTokenExpiration = DateTime.UnixEpoch.AddSeconds(secSinceEpoch);
            }

            return GenerateVsSaaSToken(
                provider,
                plan,
                scopes,
                "microsoft",
                tid,
                oid,
                username,
                displayName,
                sourceTokenExpiration,
                requestedExpiration,
                logger);
        }

        /// <summary>
        /// Generates a delegated VsSaaS token.
        /// </summary>
        /// <param name="provider">The token provider.</param>
        /// <param name="plan">The plan the token applies to.</param>
        /// <param name="scopes">The scope claim of the token.</param>
        /// <param name="identity">The identity of the user the token is granted to.</param>
        /// <param name="armTokenExpiration">The expiration of the source ARM token.</param>
        /// <param name="requestedExpiration">The expiration of the token.</param>
        /// <param name="logger">logger.</param>
        /// <returns>security token.</returns>
        public static string GenerateDelegatedVsSaaSToken(
            this ITokenProvider provider,
            VsoPlan plan,
            string[] scopes,
            DelegateIdentity identity,
            DateTime? armTokenExpiration,
            DateTime? requestedExpiration,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(identity, nameof(identity));

            return GenerateVsSaaSToken(
                provider,
                plan,
                scopes,
                "vso", // TODO - check for known providers (e.g. github)
                plan.Id,
                identity.Id ?? identity.Username, // TODO - make Id a required field (requires a swagger update)
                identity.Username,
                identity.DisplayName,
                armTokenExpiration,
                requestedExpiration,
                logger);
        }

        private static string GenerateVsSaaSToken(
            ITokenProvider provider,
            VsoPlan plan,
            string[] scopes,
            string providerId,
            string tid,
            string oid,
            string username,
            string displayName,
            DateTime? sourceTokenExpiration,
            DateTime? requestedExpiration,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(plan, nameof(plan));
            Requires.NotNull(scopes, nameof(scopes));
            Requires.NotNull(providerId, nameof(providerId));
            Requires.NotNull(tid, nameof(tid));
            Requires.NotNull(oid, nameof(oid));
            Requires.NotNull(username, nameof(username));
            Requires.NotNull(displayName, nameof(displayName));
            Requires.NotNull(logger, nameof(logger));

            sourceTokenExpiration = null; // TODO - respect this setting, ignoring now to unblock

            var expiresAt = requestedExpiration ?? DateTime.UtcNow.Add(VsSaaSTokenExpiration);
            if (sourceTokenExpiration.HasValue && sourceTokenExpiration.Value < expiresAt)
            {
                expiresAt = sourceTokenExpiration.Value;
            }

            var serializedScopes = string.Join(" ", scopes);

            logger.AddValue("jwt_plan", plan.Id);
            logger.AddValue("jwt_plan_resource", plan.Plan.ResourceId);
            logger.AddValue("jwt_scopes", serializedScopes);

            var claims = new List<Claim>
            {
                new Claim(CustomClaims.Provider, providerId),
                new Claim(CustomClaims.TenantId, tid),
                new Claim(CustomClaims.OId, oid),
                new Claim(CustomClaims.Username, username),
                new Claim(CustomClaims.DisplayName, displayName),
                new Claim(CustomClaims.PlanResourceId, plan.Plan.ResourceId),
                new Claim(CustomClaims.Scope, serializedScopes),
            };

            if (username.Contains("@"))
            {
                claims.Add(new Claim(CustomClaims.Email, username));
            }

            var token = provider.JwtWriter.WriteToken(
                logger,
                provider.Settings.VsSaaSTokenSettings.Issuer,
                provider.Settings.VsSaaSTokenSettings.Audience,
                expiresAt,
                claims.ToArray());

            return token;
        }
    }
}
