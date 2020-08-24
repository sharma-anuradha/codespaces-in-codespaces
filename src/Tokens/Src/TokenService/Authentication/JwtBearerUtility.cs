// <copyright file="JwtBearerUtility.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.AspNetCore.Authentication.JwtBearer;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.TokenService.Settings;
using static Microsoft.VsSaaS.AspNetCore.Authentication.JwtBearer.JwtBearerAuthenticationOptions2;

namespace Microsoft.VsSaaS.Services.TokenService.Authentication
{
    /// <summary>
    /// Support for JWT Bearer authentication.
    /// </summary>
    public static class JwtBearerUtility
    {
        /// <summary>
        /// The authentication scheme for AAD tokens.
        /// </summary>
        public const string AadAuthenticationScheme = "aad";

        /// <summary>
        /// The authentication scheme for GitHub tokens.
        /// </summary>
        public const string GithubAuthenticationScheme = "github";

        /// <summary>
        /// All authentication schemes supported by the token service.
        /// </summary>
        public const string AllAuthenticationSchemes =
            AadAuthenticationScheme + "," + GithubAuthenticationScheme;

        /// <summary>Service principals that issue tokens must be given this role.</summary>
        public const string IssuerRole = "Issuer";

        /// <summary>Service principals that validate tokens must be given this role.</summary>
        public const string ValidatorRole = "Validator";

        /// <summary>
        /// Configure the <see cref="JwtBearerAuthenticationOptions2"/> object for AAD auth.
        /// </summary>
        /// <param name="jwtBearerOptions">The options instance.</param>
        public static void ConfigureAadOptions(JwtBearerAuthenticationOptions2 jwtBearerOptions)
        {
            jwtBearerOptions.Events = new JwtBearerEvents
            {
                OnTokenValidated = TokenValidatedAsync,
                OnAuthenticationFailed = AuthenticationFailedAsync,
            };
            jwtBearerOptions.IsEmailClaimRequired = false;
            jwtBearerOptions.CompatibilityAudiences = new Audience[]
            {
                Audience.VisualStudioClient,
            };
        }

        /// <summary>
        /// Handle JWT Bearer token valiation.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <returns>Async task.</returns>
        public static Task TokenValidatedAsync(TokenValidatedContext context)
        {
            var identity = (ClaimsIdentity)context.Principal.Identity;
            try
            {
                var appId = identity.GetClientAppid();
                if (!string.IsNullOrEmpty(appId))
                {
                    var services = context.HttpContext.RequestServices;
                    var appSettings = services.GetRequiredService<TokenServiceAppSettings>();
                    var clientSettings = appSettings.ClientSettings.Values.FirstOrDefault((cs) =>
                        cs.AppIds?.Contains(appId, StringComparer.OrdinalIgnoreCase) == true);
                    context.HttpContext.SetClientSettings(clientSettings);

                    if (clientSettings != null)
                    {
                        identity.AddClaim(
                            new Claim(ClaimsIdentity.DefaultRoleClaimType, ValidatorRole));

                        if (clientSettings.ValidIssuers != null)
                        {
                            identity.AddClaim(
                                new Claim(ClaimsIdentity.DefaultRoleClaimType, IssuerRole));
                        }
                    }
                    else
                    {
                        // There is an appid claim, but it is not a known service.
                        // Log the appid to assist with troubleshooting configuration issues.
                        var logger = services.GetRequiredService<IDiagnosticsLogger>();
                        logger.AddValue("appid", appId);
                        logger.LogInfo("token_auth_unknown_appid");
                    }
                }
            }
            catch (Exception ex)
            {
                var services = context.HttpContext.RequestServices;
                var logger = services.GetRequiredService<IDiagnosticsLogger>();
                logger.LogException("token_auth_error", ex);
                context.Fail(ex.Message);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle JWT Bearer authentication failed.
        /// </summary>
        /// <param name="context">Authentication failed context.</param>
        /// <returns>Async task.</returns>
        public static async Task AuthenticationFailedAsync(AuthenticationFailedContext context)
        {
            await Task.CompletedTask;
            var env = ApplicationServicesProvider.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
                var httpContext = context.HttpContext;
                var logger = httpContext.GetLogger();
                logger?.AddExceptionInfo(context.Exception).LogWarning("jwt_authentication_failed");
            }
        }
    }
}
