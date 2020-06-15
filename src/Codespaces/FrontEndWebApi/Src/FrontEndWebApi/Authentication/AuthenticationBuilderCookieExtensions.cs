// <copyright file="AuthenticationBuilderCookieExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.AspNetCore.Authentication;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// <see cref="AuthenticationBuilder"/> cookie extensions.
    /// </summary>
    public static class AuthenticationBuilderCookieExtensions
    {
        /// <summary>
        /// The Cookie authentication scheme.
        /// </summary>
        public const string AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        /// <summary>
        /// Add Cookie authentication.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <returns>The <paramref name="builder"/> instance.</returns>
        public static AuthenticationBuilder AddVsSaaSCookieBearer(
            this AuthenticationBuilder builder)
        {
            builder
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.Cookie.Name = ".AspNet.SharedCookie";
                    options.Events.OnRedirectToLogin = ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    };
                    options.Events.OnValidatePrincipal = CookieValidatedPrincipalAsync;
                });

            return builder;
        }

        private static async Task CookieValidatedPrincipalAsync(CookieValidatePrincipalContext context)
        {
            var httpContext = context.HttpContext;
            var principal = context.Principal;

            // Use the same algorithm with Cookies as with JWT Bearer.
            var isEmailClaimRequired = AuthenticationConstants.IsEmailClaimRequired;
            if (!httpContext.SetUserContextFromClaimsPrincipal(principal, isEmailClaimRequired, out _))
            {
                context.RejectPrincipal();
                return;
            }

            try
            {
                var validatedPrincipalIdentityHandler = httpContext.RequestServices.GetRequiredService<IValidatedPrincipalIdentityHandler>();
                await validatedPrincipalIdentityHandler.ValidatedPrincipalAsync(principal, null);
            }
            catch (Exception ex)
            {
                var logger = httpContext.GetLogger();
                logger.LogException("cookie_authentication_error", ex);
                context.RejectPrincipal();
            }
        }
    }
}
