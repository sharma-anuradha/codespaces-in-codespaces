// <copyright file="AuthenticationBuilderRPSaasExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
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

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// <see cref="AuthenticationBuilder"/> extensions.
    /// </summary>
    public static class AuthenticationBuilderRPSaasExtensions
    {
        /// <summary>
        /// The authentication scheme for calls from RP-SaaS.
        /// </summary>
        public const string AuthenticationScheme = "aadrpsaas";

        private static readonly string TenantClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

        private static string Issuer { get; } = "https://sts.windows.net/";

        private static IEnumerable<string> DefaultAudiences { get; } = new string[]
        {
            "https://management.core.windows.net/",
        };

        /// <summary>
        /// Add RPSaaS specific Jwt Bearer.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// /// <param name="authority">The RPSaaS Authority URL.</param>
        /// <returns>the instance of builder.</returns>
        public static AuthenticationBuilder AddRPSaaSJwtBearer(this AuthenticationBuilder builder, string authority)
        {
            builder
               .AddJwtBearer(AuthenticationScheme, options =>
               {
                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidAudiences = DefaultAudiences,
                       ValidateIssuer = false,
                       RequireExpirationTime = true,
                       ValidateLifetime = true,
                       ValidateIssuerSigningKey = true,
                   };
                   options.Authority = authority;
                   options.Events = new JwtBearerEvents
                   {
                       OnAuthenticationFailed = AuthenticationFailedAsync,
                       OnTokenValidated = OnTokenValidated,
                   };
               });

            return builder;
        }

        /// <summary>
        /// Method to be called in the event of failed authentication.
        /// </summary>
        /// <param name="arg">failure context.</param>
        /// <returns>Task.</returns>
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
                .LogInfo("jwt_authentication_failed");
        }

        /// <summary>
        /// Method to be called after security token is validated.
        /// </summary>
        /// <param name="context">TokenValidate context.</param>
        /// <returns>Task.</returns>
        private static async Task OnTokenValidated(TokenValidatedContext context)
        {
            var identity = context.Principal;
            var tenantClaim = identity.Claims.FirstOrDefault(j => j.Type == TenantClaimType);
            var issuerClaim = identity.Claims.FirstOrDefault(i => i.Type == "iss");

            // Construct the fully tenant qualified issuer url.
            var issuerFull = Issuer + tenantClaim.Value + "/";
            if (issuerClaim.Value != issuerFull)
            {
                // fail request
                var logger = context.HttpContext.GetLogger() ?? new JsonStdoutLogger(new LogValueSet());
                logger
                    .FluentAddValue("Scheme", context.Scheme.Name)
                    .FluentAddValue("Audience", context.Options.Audience)
                    .FluentAddValue("Authority", context.Options.Authority)
                    .FluentAddValue("RequestUri", context.Request.GetDisplayUrl())
                    .FluentAddValue("PrincipalIdentityName", context.Principal?.Identity.Name)
                    .FluentAddValue("PrincipalIsAuthenticationType", context.Principal?.Identity.AuthenticationType)
                    .FluentAddValue("PrincipalIsAuthenticated", context.Principal?.Identity.IsAuthenticated.ToString())
                    .LogInfo("jwt_issuer_notmatched");

                context.Fail("Issuer claim did not match expected claim");
            }

            await Task.CompletedTask;
        }
    }
}
