// <copyright file="JwtBearerUtility.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.AspNetCore.Authentication.JwtBearer;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// Support for JWT Bearer authentication.
    /// </summary>
    public static class JwtBearerUtility
    {
        /// <summary>
        /// The JWT authentication scheme for AAD tokens.
        /// </summary>
        public const string AadAuthenticationScheme = "aad";

        /// <summary>
        /// The JWT authentication scheme for VSO tokens.
        /// </summary>
        public const string VsoAuthenticationScheme = "vso";

        /// <summary>
        /// List of supported schemes for authenticating users.
        /// </summary>
        public const string UserAuthenticationSchemes =
            AadAuthenticationScheme + "," + VsoAuthenticationScheme;

        /// <summary>
        /// Configure the <see cref="JwtBearerAuthenticationOptions2"/> object for AAD auth.
        /// </summary>
        /// <param name="jwtBearerOptions">The options instance.</param>
        internal static void ConfigureAadOptions(JwtBearerAuthenticationOptions2 jwtBearerOptions)
        {
            jwtBearerOptions.IsEmailClaimRequired = AuthenticationConstants.IsEmailClaimRequired;

            if (AuthenticationConstants.UseCompatibilityAudiences)
            {
                // For backwards compatibility, continue to support the older auidences as well.
                // Supporting tokens for these audiences can lead to the same user obtaining different
                // profile records, and different user ids, because the tokens can present different claims
                // depending on the token audience configuration.
                jwtBearerOptions.CompatibilityAudiences = new List<JwtBearerAuthenticationOptions2.Audience>
                {
                    // VS Cloud Services (DEV) 3rd party appid
                    #pragma warning disable CS0618 // Type or member is obsolete
                    JwtBearerAuthenticationOptions2.Audience.VisualStudioServicesApiDev,
                    #pragma warning restore CS0618 // Type or member is obsolete

                    // VS Client Audience (for legacy/testing reasons)
                    JwtBearerAuthenticationOptions2.Audience.VisualStudioClient,
                };

                if (System.Diagnostics.Debugger.IsAttached)
                {
                    jwtBearerOptions.EnableDevelopmentDiagnostics = true;
                }
            }

            jwtBearerOptions.Events = new JwtBearerEvents
            {
                OnTokenValidated = TokenValidatedAsync,
                OnAuthenticationFailed = AuthenticationFailedAsync,
            };
        }

        /// <summary>
        /// Add VSO (Cascade token) JWT bearer authentication.
        /// </summary>
        /// <param name="builder">Authentication builder.</param>
        internal static void AddVsoJwtBearerAuthentication(this AuthenticationBuilder builder)
        {
            var jwtReader = new JwtReader();

            builder
                .AddJwtBearer(VsoAuthenticationScheme, options =>
                {
                    var logger = ApplicationServicesProvider.GetRequiredService<IDiagnosticsLogger>();

                    options.TokenValidationParameters = jwtReader.GetValidationParameters(logger);

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = AuthenticationFailedAsync,
                        OnTokenValidated = TokenValidatedAsync,
                    };
                })
                .Services
                .AddTokenSettingsToJwtReader(jwtReader, (authSettings) => authSettings.VsSaaSTokenSettings);
        }

        /// <summary>
        /// Handle JWT Bearer token valiation.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <returns>Async task.</returns>
        internal static async Task TokenValidatedAsync(TokenValidatedContext context)
        {
            try
            {
                var validatedPrincipalHandler = context.HttpContext.RequestServices.GetService<IValidatedPrincipalIdentityHandler>();
                var newPrincipal = await validatedPrincipalHandler.ValidatedPrincipalAsync(context.Principal, context.SecurityToken as JwtSecurityToken);

                context.Principal = newPrincipal;
            }
            catch (Exception ex)
            {
                context.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Handle JWT Bearer authentication failed.
        /// </summary>
        /// <param name="context">Authentication failed context.</param>
        /// <returns>Async task.</returns>
        internal static async Task AuthenticationFailedAsync(AuthenticationFailedContext context)
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
