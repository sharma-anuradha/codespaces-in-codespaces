//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using VsClk.EnvReg.Repositories;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Authentication
{
    public static class AuthenticationBuilderVsClkCookieExtension
    {
        public const string AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        public static AuthenticationBuilder AddVsClkCookieBearer(
            this AuthenticationBuilder builder,
            AppSettings appSettings)
        {
            builder
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.Cookie.Name = ".AspNet.SharedCookie";
                    options.Events.OnRedirectToLogin = ctx =>
                    {
                        ctx.Response.StatusCode = 401;
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
