// <copyright file="AuthenticationBuilderCookieExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

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
            // Make sure the cookie has the claim we want
            var userId = context.Principal.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                context.RejectPrincipal();
                return;
            }

            // Locate needed services
            var logger = context.HttpContext.GetLogger();
            var profileRepository = context.HttpContext.RequestServices.GetService<IProfileRepository>();
            var currentUserProvider = context.HttpContext.RequestServices.GetService<ICurrentUserProvider>();

            // Make the user's id, display name, email easier to access over the lifetime of the request
            var profile = await profileRepository.GetCurrentUserProfileAsync(logger);
            if (profile == null)
            {
                context.RejectPrincipal();
                return;
            }
            else
            {
                currentUserProvider.SetProfile(profile);
            }
        }
    }
}
