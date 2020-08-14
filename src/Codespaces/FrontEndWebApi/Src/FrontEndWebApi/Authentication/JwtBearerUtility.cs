// <copyright file="JwtBearerUtility.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
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
        /// The authentication scheme for GitHub tokens.
        /// </summary>
        public const string GithubAuthenticationScheme = "github";

        /// <summary>
        /// List of supported schemes for authenticating users.
        /// </summary>
        public const string UserAuthenticationSchemes =
            AadAuthenticationScheme + "," + VsoAuthenticationScheme + "," + GithubAuthenticationScheme;

        /// <summary>
        /// Configure the <see cref="JwtBearerAuthenticationOptions2"/> object for AAD auth.
        /// </summary>
        /// <param name="jwtBearerOptions">The options instance.</param>
        public static void ConfigureAadOptions(JwtBearerAuthenticationOptions2 jwtBearerOptions)
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
        /// <returns>The AuthenticationBuilder to enable chaining.</returns>
        public static AuthenticationBuilder AddVsoJwtBearerAuthentication(this AuthenticationBuilder builder)
        {
            var jwtReader = new JwtReader();

            builder
                .AddJwtBearer(VsoAuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = jwtReader.GetValidationParameters(
                        () => ApplicationServicesProvider.GetRequiredService<IDiagnosticsLogger>());

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = AuthenticationFailedAsync,
                        OnTokenValidated = TokenValidatedAsync,
                    };
                })
                .Services
                .AddSingleton<ICascadeTokenReader>(new CascadeTokenReader(jwtReader))
                .AddTokenSettingsToJwtReader(jwtReader, (authSettings) => authSettings.VsSaaSTokenSettings);

            return builder;
        }

        /// <summary>
        /// Handle JWT Bearer token valiation.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <returns>Async task.</returns>
        internal static async Task TokenValidatedAsync(TokenValidatedContext context)
        {
            var logger = context.HttpContext.GetLogger() ?? new JsonStdoutLogger(new LogValueSet());

            logger.AddAuthenticationResultContext(context);

            try
            {
                var validatedPrincipalHandler = context.HttpContext.RequestServices.GetService<IValidatedPrincipalIdentityHandler>();
                var newPrincipal = await validatedPrincipalHandler.ValidatedPrincipalAsync(context.Principal, context.SecurityToken as JwtSecurityToken, logger.NewChildLogger());

                // Apply new principal
                context.Principal = newPrincipal;

                logger.LogInfo("jwt_authentication_success");
            }
            catch (Exception ex)
            {
                logger.AddExceptionInfo(ex).LogError("jwt_authentication_error");
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

            var httpContext = context.HttpContext;
            var logger = httpContext.GetLogger() ?? new JsonStdoutLogger(new LogValueSet());

            // Only log a warning as other authentication schemes may still succeed
            logger
                .AddAuthenticationResultContext(context)
                .AddExceptionInfo(context.Exception)
                .LogWarning("jwt_authentication_failed");
        }

        private class CascadeTokenReader : ICascadeTokenReader
        {
            private readonly IJwtReader jwtReader;

            public CascadeTokenReader(IJwtReader jwtReader)
            {
                this.jwtReader = Requires.NotNull(jwtReader, nameof(jwtReader));
            }

            public ClaimsPrincipal ReadTokenPrincipal(string accessToken, IDiagnosticsLogger logger)
                => this.jwtReader.ReadTokenPrincipal(accessToken, logger);
        }
    }
}
