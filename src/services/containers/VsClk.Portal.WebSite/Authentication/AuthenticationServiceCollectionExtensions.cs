using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.AspNetCore.Authentication;
using Microsoft.VsSaaS.AspNetCore.Authentication.JwtBearer;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using StackExchange.Redis;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Common.Identity;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Authentication
{
    public static class AuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddPortalWebSiteAuthentication(
            this IServiceCollection services,
            IWebHostEnvironment env,
            AppSettings appSettings)
        {
            // Add Data protection
            if (!appSettings.IsLocal)
            {
                var redis = ConnectionMultiplexer.Connect(appSettings.VsClkRedisConnectionString);
                services.AddDataProtection()
                    .SetApplicationName("VS Sass")
                    .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
            }
            else
            {
                services.AddDataProtection()
                    .SetApplicationName("VS Sass");
            }

            // Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/signout";
                options.AccessDeniedPath = "/accessdenied";
                options.Cookie.Name = "vssaas.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Events.OnRedirectToLogin = ctx =>
                {
                    var logger = ctx.HttpContext.GetLogger();

                    logger
                        .FluentAddValue("CookieNames", string.Join(", ", ctx.Request.Cookies.Keys))
                        .LogInfo("cookie_onredirecttologin");

                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    var logger = ctx.HttpContext.GetLogger();

                    logger
                        .FluentAddValue("CookieNames", string.Join(", ", ctx.Request.Cookies.Keys))
                        .LogInfo("cookie_onredirecttoaccessdenied");

                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = CookieValidatedAsync;
            })
            .AddJwtBearerAuthentication2(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.CompatibilityAudiences = new List<JwtBearerAuthenticationOptions2.Audience>
                    { 
                        #pragma warning disable CS0618 // Type or member is obsolete
                        JwtBearerAuthenticationOptions2.Audience.VisualStudioServicesApiDev,
                        #pragma warning restore CS0618 // Type or member is obsolete
                    };

                    options.IsEmailClaimRequired = true;

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = JwtTokenValidatedAsync,
                    };
                });

            return services;
        }

        private static async Task JwtTokenValidatedAsync(TokenValidatedContext context)
        {
            var principal = context.Principal;
            var jwtSecurityToken = (JwtSecurityToken)context.SecurityToken;
            await ValidatedPrincipalAsync(principal, jwtSecurityToken);
        }

        private static async Task CookieValidatedAsync(CookieValidatePrincipalContext context)
        {
            await Task.CompletedTask;

            var httpContext = context.HttpContext;
            var principal = context.Principal;

            /*
            // Use the same algorithm with Cookies as with JWT Bearer.
            const bool isEmailClaimRequired = true;
            if (!httpContext.SetUserContextFromClaimsPrincipal(principal, isEmailClaimRequired, out _))
            {
                context.RejectPrincipal();
                return;
            }

            try
            {
                await ValidatedPrincipalAsync(principal, null);
            }
            catch (Exception ex)
            {
                logger.LogException("cookie_authentication_error", ex);
                context.RejectPrincipal();
            }
            */

            // TODO - debug only, remove these

            var logger = httpContext.GetLogger();

            logger.LogInfo("cookie_validated");

            if (principal.FindFirstValue("exp") is string value &&
                int.TryParse(value, out int exp))
            {
                var expTime = DateTime.UnixEpoch.AddSeconds(exp);
                logger.FluentAddValue("token_exp", expTime.ToString());
            }

            logger.LogInfo("cookie_claims");

            try
            {
                // Use the same algorithm with Cookies as with JWT Bearer.
                const bool isEmailClaimRequired = true;
                if (!httpContext.SetUserContextFromClaimsPrincipal(principal, isEmailClaimRequired, out _))
                {
                    logger.LogInfo("cookie_set_user_error");
                    context.RejectPrincipal();
                    return;
                }

                logger.LogInfo("cookie_set_user_success");
            }
            catch (Exception ex)
            {
                logger.LogException("cookie_authentication_error", ex);
                context.RejectPrincipal();
                return;
            }
        }

        private static async Task ValidatedPrincipalAsync(ClaimsPrincipal principal, JwtSecurityToken token)
        {
            /*
             TODO This code should get reconciled with src\FrontEnd\FrontEndWebApi\Src\FrontEndWebApi\Authentication\ValidatedPrincipalIdentityHandler.cs 
             if it is nececssary to deal with ambiguous MSA user identities.
             */
            await Task.CompletedTask;
        }
    }
}
