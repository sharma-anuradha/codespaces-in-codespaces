//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using VsClk.EnvReg.Models.DataStore;
using VsClk.EnvReg.Repositories;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Authentication
{
    public static class AuthenticationBuilderVsClkJwtExtension
    {
        private const string BadTokenMessage = "jwt_bearer_bad_token";
        private static IList<string> DefaultAudiences = new List<string> {
                "872cd9fa-d31f-45e0-9eab-6e460a02d1f1", // VS Client Audience (for legacy/testing reasons)
                "9db1d849-f699-4cfb-8160-64bed3335c72"  // VS Cloud Services
            };
        public const string AuthenticationScheme = "aad";

        public static AuthenticationBuilder AddVsClkJwtBearer(
            this AuthenticationBuilder builder,
            AppSettings appSettings)
        {
            var configAudiences = appSettings.AuthJwtAudiences?.Split(',');
            var audiences = configAudiences ?? DefaultAudiences;

            builder
                .AddJwtBearer(AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudiences = audiences,
                        ValidateIssuer = false, // Don't validate the issuer. It could come from any AAD/OrgId or from MSA.
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
                    };
                    options.Authority = "https://login.microsoftonline.com/common/v2.0";
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = AuthenticationFailedAsync,
                        OnTokenValidated = TokenValidatedAsync,
                    };
                });

            return builder;
        }

        private static Task AuthenticationFailedAsync(AuthenticationFailedContext context)
        {
            return Task.CompletedTask;
        }

        private static async Task TokenValidatedAsync(TokenValidatedContext context)
        {
            // Locate needed services
            var logger = context.HttpContext.GetLogger();
            var profileRepository = context.HttpContext.RequestServices.GetService<IProfileRepository>();
            var currentProfile = context.HttpContext.RequestServices.GetService<ICurrentUserProvider>();

            // Make the user's id, display name, email easier to access over the lifetime of the request
            var jwtToken = context.SecurityToken as JwtSecurityToken;
            if (!string.IsNullOrEmpty(jwtToken?.RawData))
            {
                currentProfile.SetBearerToken(jwtToken.RawData);

                var profile = await profileRepository.GetCurrentUserProfileAsync(logger);
                if (profile != null
                    && profile.IsCloudEnvironmentsPreviewUser())
                {
                    currentProfile.SetProfile(profile);
                    return;
                }
            }

            context.Fail(BadTokenMessage);
        }
    }
}
