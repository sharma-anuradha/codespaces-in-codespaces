using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using Microsoft.VsSaaS.AspNetCore.Authentication;
using Microsoft.VsSaaS.AspNetCore.Authentication.JwtBearer;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Tokens;
using StackExchange.Redis;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Authentication
{
    public static class AuthenticationServiceCollectionExtensions
    {
        public const string VsoAuthenticationScheme = "vso";
        public const string VsoBodyAuthenticationScheme = "vso-body";
        public const string CookieNoSameSiteScheme = "cookies-no-same-site-scheme";

        public const string JwtBearerAuthenticationSchemes =
            JwtBearerDefaults.AuthenticationScheme + "," + VsoAuthenticationScheme;

        public const string CookeAuthenticationSchemes =
            CookieAuthenticationDefaults.AuthenticationScheme + "," + CookieNoSameSiteScheme;

        public static IServiceCollection AddPortalWebSiteAuthentication(
            this IServiceCollection services,
            AppSettings appSettings)
        {
            // Add Data protection
            if (!appSettings.IsLocal && !appSettings.IsTest)
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
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerAuthenticationSchemes;
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookieAuthentication(CookieAuthenticationDefaults.AuthenticationScheme, true)
                .AddCookieAuthentication(CookieNoSameSiteScheme, false)
                .AddAadAuthentication()
                .AddVsoAuthentication(appSettings, VsoAuthenticationScheme, false)
                .AddVsoAuthentication(appSettings, VsoBodyAuthenticationScheme, true);

            return services;
        }

        private static AuthenticationBuilder AddCookieAuthentication(
            this AuthenticationBuilder builder, string scheme, bool isSameSite)
        {
            return builder.AddCookie(scheme, options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/signout";
                options.AccessDeniedPath = "/accessdenied";
                options.Cookie.Name = "vssaas.session";
                options.Cookie.SameSite = (isSameSite) ? SameSiteMode.Lax : SameSiteMode.None;
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
            });
        }

        private static AuthenticationBuilder AddAadAuthentication(
            this AuthenticationBuilder builder)
        {
            return builder.AddJwtBearerAuthentication2(
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
        }

        private static AuthenticationBuilder AddVsoAuthentication(
            this AuthenticationBuilder builder,
            AppSettings appSettings, string scheme, bool isBody)
        {
            var cascadeJwtReader = new JwtReader();

            builder.AddJwtBearer(
                scheme,
                options =>
                {
                    var logger = ApplicationServicesProvider.GetRequiredService<IDiagnosticsLogger>();

                    options.TokenValidationParameters = cascadeJwtReader.GetValidationParameters(logger);

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = JwtTokenValidatedAsync,
                    };

                    if (isBody) {
                        options.Events.OnMessageReceived = OnVSOBodyAuthenticationMessage;
                    }
                })
                .Services
                .AddSingleton<IAsyncWarmup>((serviceProvider) =>
                {
                    var keyVaultReader = ApplicationServicesProvider.GetRequiredService<IKeyVaultSecretReader>();
                    var logger = ApplicationServicesProvider.GetRequiredService<IDiagnosticsLogger>();

                    var certCache = new JwtCertificateCredentialsKeyVaultCache(
                        keyVaultReader,
                        ClientKeyvaultReader.GetAppKeyVaultName(),
                        appSettings.VsSaaSCertificateSecretName,
                        logger);
                    certCache.StartPeriodicRefresh(TimeSpan.FromDays(1));

                    cascadeJwtReader.AddIssuer(appSettings.VsSaaSTokenIssuer, certCache.ConvertToPublic());
                    cascadeJwtReader.AddAudience(appSettings.VsSaaSTokenIssuer); // Same as issuer

                    return certCache;
                });

            return builder;
        }

        private static async Task OnVSOBodyAuthenticationMessage(MessageReceivedContext context)
        {

            string authorization = context.Request.Headers["Authorization"];

            // If no authorization header found, nothing to process further
            if (!string.IsNullOrEmpty(authorization))
            {
                context.NoResult();
                return;
            }

            var reader = new StreamReader(context.Request.Body);
            var rawMessage = await reader.ReadToEndAsync();

            try {
                var bodyParams = HttpUtility.ParseQueryString(rawMessage);
                var cascadeToken = bodyParams.Get("cascadeToken");

                if (string.IsNullOrWhiteSpace(cascadeToken)) {
                    context.NoResult();
                    return;
                }

                context.Token = cascadeToken;
            } catch (Exception) {
                context.NoResult();
            }
        }

        private static async Task JwtTokenValidatedAsync(TokenValidatedContext context)
        {
            var principal = context.Principal;

            // Verify email claim exists
            principal.Identities.First().GetUserEmail(isEmailClaimRequired: true);

            var jwtSecurityToken = (JwtSecurityToken)context.SecurityToken;
            await ValidatedPrincipalAsync(principal, jwtSecurityToken);
        }

        private static async Task CookieValidatedAsync(CookieValidatePrincipalContext context)
        {
            await Task.CompletedTask;

            var httpContext = context.HttpContext;
            var principal = context.Principal;

            if (principal.FindFirstValue("exp") is string value &&
                int.TryParse(value, out int exp))
            {
                var expTime = DateTime.UnixEpoch.AddSeconds(exp);
            }

            try
            {
                // Use the same algorithm with Cookies as with JWT Bearer.
                const bool isEmailClaimRequired = true;
                if (!httpContext.SetUserContextFromClaimsPrincipal(principal, isEmailClaimRequired, out _))
                {
                    context.RejectPrincipal();
                    return;
                }
            }
            catch (Exception)
            {
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
