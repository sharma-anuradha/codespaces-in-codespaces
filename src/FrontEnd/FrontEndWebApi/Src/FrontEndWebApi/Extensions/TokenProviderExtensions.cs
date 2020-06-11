// <copyright file="TokenProviderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions
{
    /// <summary>
    /// Extensions adding specific token functionality to <see cref="ITokenProvider"/>.
    /// </summary>
    public static class TokenProviderExtensions
    {
        private static readonly TimeSpan DefaultVsSaaSTokenLifetime = TimeSpan.FromDays(25);

        /// <summary>
        /// Generates a token that grants the caller access to VSO environment services.
        /// </summary>
        /// <param name="provider">The token provider.</param>
        /// <param name="plan">The plan the token applies to.</param>
        /// <param name="scopes">The scope claim of the token.</param>
        /// <param name="identity">The identity of the user the token is granted to.</param>
        /// <param name="requestedExpiration">The expiration of the token.</param>
        /// <param name="logger">logger.</param>
        /// <returns>security token.</returns>
        public static async Task<string> GenerateVsSaaSTokenAsync(
            this ITokenProvider provider,
            VsoPlan plan,
            string[] scopes,
            ClaimsIdentity identity,
            DateTime? requestedExpiration,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(identity, nameof(identity));

            var tid = identity.GetTenantId();
            var oid = identity.GetObjectId();
            var username = identity.GetPreferredUserName()
                ?? identity.GetUserEmail(isEmailClaimRequired: false);
            var displayName = identity.GetUserDisplayName();

            var expiration = identity.Claims.FirstOrDefault((c) => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            DateTime? sourceTokenExpiration = null;
            if (int.TryParse(expiration, out int secSinceEpoch))
            {
                sourceTokenExpiration = DateTime.UnixEpoch.AddSeconds(secSinceEpoch);
            }

            return await GenerateVsSaaSTokenAsync(
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
        /// Generates a token that grants a caller's delegate access to VSO environment services.
        /// </summary>
        /// <param name="provider">The token provider.</param>
        /// <param name="plan">The plan the token applies to.</param>
        /// <param name="partner">The partner requesting the token, if they are known.</param>
        /// <param name="scopes">The scope claim of the token.</param>
        /// <param name="identity">The identity of the user the token is granted to.</param>
        /// <param name="armTokenExpiration">The expiration of the source ARM token.</param>
        /// <param name="requestedExpiration">The expiration of the token.</param>
        /// <param name="environmentIds">Optional list of environment IDs that the token should be
        /// scoped to.</param>
        /// <param name="logger">logger.</param>
        /// <returns>security token.</returns>
        public static async Task<string> GenerateDelegatedVsSaaSTokenAsync(
            this ITokenProvider provider,
            VsoPlan plan,
            Partner? partner,
            string[] scopes,
            DelegateIdentity identity,
            DateTime? armTokenExpiration,
            DateTime? requestedExpiration,
            string[] environmentIds,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(identity, nameof(identity));

            string providerId;
            IEnumerable<Claim> extraClaims = Enumerable.Empty<Claim>();

            if (partner == Partner.GitHub)
            {
                providerId = "github";

                if (!IsEmail(identity.Username))
                {
                    var githubUserEmail = $"{identity.Username}@users.noreply.github.com";
                    extraClaims = extraClaims.Concat(
                        new[] { new Claim(CustomClaims.Email, githubUserEmail), });
                }
            }
            else
            {
                providerId = "vso";
            }

            if (environmentIds != null)
            {
                extraClaims = extraClaims.Concat(
                    environmentIds.Select((e) => new Claim(CustomClaims.Environments, e)));
            }

            return await GenerateVsSaaSTokenAsync(
                provider,
                plan,
                scopes,
                providerId,
                plan.Id,
                identity.Id,
                identity.Username,
                identity.DisplayName,
                armTokenExpiration,
                requestedExpiration,
                logger,
                extraClaims);
        }

        private static async Task<string> GenerateVsSaaSTokenAsync(
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
            IDiagnosticsLogger logger,
            IEnumerable<Claim> extraClaims = null)
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

            var expiresAt = requestedExpiration ?? DateTime.UtcNow.Add(
                provider.Settings.VsSaaSTokenSettings.Lifetime ?? DefaultVsSaaSTokenLifetime);
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

            if (IsEmail(username))
            {
                claims.Add(new Claim(CustomClaims.Email, username));
            }

            if (extraClaims != null)
            {
                claims.AddRange(extraClaims);
            }

            var token = await provider.IssueTokenAsync(
                provider.Settings.VsSaaSTokenSettings.Issuer,
                provider.Settings.VsSaaSTokenSettings.Audience,
                expiresAt,
                claims,
                logger);
            return token;
        }

        private static bool IsEmail(string maybeEmail)
        {
            return maybeEmail.Contains('@');
        }
    }
}
