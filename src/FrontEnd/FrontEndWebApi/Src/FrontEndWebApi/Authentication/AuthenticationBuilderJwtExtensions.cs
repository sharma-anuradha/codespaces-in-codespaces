// <copyright file="AuthenticationBuilderJwtExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// <see cref="AuthenticationBuilder"/> extensions.
    /// </summary>
    public static class AuthenticationBuilderJwtExtensions
    {
        /// <summary>
        /// The JWT authentication scheme.
        /// </summary>
        public const string AuthenticationScheme = "aad";

        private const string BadTokenMessage = "jwt_bearer_bad_token";

        // TODO: Get rid of DefaultAudiences, unless replacing them with new Microsoft.VsSaaS.Common.AppIds
        private static IEnumerable<string> DefaultAudiences { get; } = new string[]
        {
            "https://management.core.windows.net/",  // live
            "aebc6443-996d-45c2-90f0-388ff96faa56",  // Visual Studio Online Client (DEV)
            "872cd9fa-d31f-45e0-9eab-6e460a02d1f1",  // VS IDE Client Audience (for legacy/testing reasons)
            "9db1d849-f699-4cfb-8160-64bed3335c72",  // Visual Studio Services API (DEV)
        };

        /// <summary>
        /// Add JWT Bearer authentication.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="jwtBearerOptions">The global application settings.</param>
        /// <returns>The <paramref name="builder"/> instance.</returns>
        public static AuthenticationBuilder AddVsSaaSJwtBearer(
            this AuthenticationBuilder builder,
            JwtBearerOptions jwtBearerOptions)
        {
            Requires.NotNull(jwtBearerOptions, nameof(jwtBearerOptions));

            var configAudiences = jwtBearerOptions.Audiences?.Split(',');
            var audiences = (configAudiences ?? new string[0])
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                .Union(DefaultAudiences) // add the default audiences
                .Distinct()
                .ToArray();

            builder
                .AddJwtBearer(AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudiences = audiences,
                        ValidateIssuer = false, // Don't validate the issuer. It could come from any AAD/OrgId or from MSA.
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                    };
                    options.Authority = jwtBearerOptions.Authority;
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = AuthenticationFailedAsync,
                        OnTokenValidated = TokenValidatedAsync,
                    };
                });

            return builder;
        }

        private static async Task AuthenticationFailedAsync(AuthenticationFailedContext arg)
        {
            await Task.CompletedTask;

            var logger = arg.HttpContext.GetLogger() ?? new JsonStdoutLogger(new LogValueSet());

            logger
                .FluentAddValue("Scheme", arg.Scheme.Name)
                .FluentAddValue("Audience", arg.Options.Audience)
                .FluentAddValue("Authority", arg.Options.Authority)
                .FluentAddValue("RequestUri", arg.Request.GetDisplayUrl())
                .FluentAddValue("PrincipalIdentityName", arg.Principal?.Identity.Name)
                .FluentAddValue("PrincipalIsAuthenticationType", arg.Principal?.Identity.AuthenticationType)
                .FluentAddValue("PrincipalIsAuthenticated", arg.Principal?.Identity.IsAuthenticated.ToString())
                .FluentAddValue("Exception", arg.Exception.Message)
                .LogWarning("jwt_authentication_failed");
        }

        private static async Task TokenValidatedAsync(TokenValidatedContext context)
        {
            // Locate needed services
            var logger = context.HttpContext.GetLogger() ?? new JsonStdoutLogger(new LogValueSet());
            var profileRepository = context.HttpContext.RequestServices.GetService<IProfileRepository>();
            var currentProfile = context.HttpContext.RequestServices.GetService<ICurrentUserProvider>();

            void JwtTokenFail(string message)
            {
                currentProfile.SetBearerToken(null);
                logger.LogErrorWithDetail("jwt_token_validation_error", message);
                context.Fail(BadTokenMessage);
            }

            var jwtToken = context.SecurityToken as JwtSecurityToken;
            if (string.IsNullOrEmpty(jwtToken?.RawData))
            {
                JwtTokenFail(BadTokenMessage);
                return;
            }

            Profile profile;
            try
            {
                // The bearer token must be set in order to read the user profile.
                currentProfile.SetBearerToken(jwtToken.RawData);
                profile = await profileRepository.GetCurrentUserProfileAsync(logger);
            }
            catch (Exception ex)
            {
                JwtTokenFail($"Cloud not get Live Share profile: {ex.Message}");
                return;
            }

            if (profile is null)
            {
                JwtTokenFail("Cloud not get Live Share profile: null");
                return;
            }

            if (!profile.IsCloudEnvironmentsPreviewUser())
            {
                JwtTokenFail("Not preview user");
                return;
            }

            // OK
            currentProfile.SetProfile(profile);
            return;
        }
    }
}
