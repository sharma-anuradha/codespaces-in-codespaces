// <copyright file="AuthenticationBuilderRPSaasExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.AspNetCore.Authentication;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.IdentityMap;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

        /// <summary>
        /// The key of HttpContext.Items which will provide the source ARM <see cref="ClaimsPrincipal"/>.
        /// </summary>
        public const string SourceArmTokenClaims = "SourceArmTokenClaims";

        private const string ArmUserTokenHeaderName = "x-ms-arm-signed-user-token";

        private const string CodespaceUserTokenHeaderName = "x-ms-codespace-user-token";

        private const string RPaaSCorrelationIdHeaderName = "x-ms-correlation-request-id";

        // This is captured during configuration - don't change any settings on it
        private static JwtBearerOptions AadBearerOptions { get; set; } = null;

        private static IEnumerable<string> DefaultAudiences { get; } = new string[]
        {
            "https://management.core.windows.net/",
            "https://management.azure.com/",
        };

        private static IJwtReader ArmUserJwtReader { get; set; } = null;

        private static RPSaaSCertifcateCache ArmIssuerCredentialCache { get; set; } = null;

        /// <summary>
        /// Add RPSaaS specific Jwt Bearer.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="settings">The RPSaaS settings.</param>
        /// <returns>the instance of builder.</returns>
        public static AuthenticationBuilder AddRPSaaSJwtBearer(this AuthenticationBuilder builder, RPSaaSSettings settings)
        {
            builder
                .AddJwtBearer(AuthenticationScheme, options =>
                {
                    ArmUserJwtReader = new JwtReader();
                    foreach (var audience in DefaultAudiences)
                    {
                        ArmUserJwtReader.AddAudience(audience);
                    }

                    var armTokenReader = new JwtReader();
                    foreach (var audience in DefaultAudiences)
                    {
                        armTokenReader.AddAudience(audience);
                    }

                    armTokenReader.AddIssuer(settings.IssuerHostname.TrimEnd('/') + "/{tenantid}/");

                    var armTokenValidationParameters = armTokenReader.GetValidationParameters(
                        () => ApplicationServicesProvider.GetRequiredService<IDiagnosticsLogger>(),
                        resolveIssuerSigningKeys: false);

                    options.TokenValidationParameters = armTokenValidationParameters;

                    options.Authority = settings.Authority;

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = AuthenticationFailedAsync,
                        OnTokenValidated = BuildOnTokenValidated(settings),
                    };
                })
                .Services
                .AddSingleton<IAsyncWarmup>((serviceProvider) =>
                {
                    var logger = serviceProvider.GetRequiredService<IDiagnosticsLogger>();

                    ArmIssuerCredentialCache = new RPSaaSCertifcateCache(settings.SignedUserTokenCertUrl, logger);

                    return ArmIssuerCredentialCache;
                })
                .Configure<JwtBearerOptions>(JwtBearerUtility.AadAuthenticationScheme, (options) =>
                {
                    AadBearerOptions = options;
                });
            return builder;
        }

        /// <summary>
        /// Checks if an ARM user identity is an MSA.
        /// </summary>
        /// <param name="identity">User identity provided by ARM.</param>
        /// <returns>True if the user identity is a Microsoft Account (MSA), else false.</returns>
        /// <remarks>
        /// MSA tokens for ARM users do not use one of the well-known MSA tenants, because the
        /// ARM user identities are in the tenant that is associated with the subscription, which
        /// for MSA users is typically a tenant created by Azure automatically as their default
        /// tenant (unless they have signed in as a guest in another tenant).
        /// </remarks>
        internal static bool IsArmMsaIdentity(ClaimsIdentity identity)
        {
            var idpClaim = identity?.FindFirst("idp")?.Value;
            var altSecIdClaim = identity?.FindFirst("altsecid")?.Value;
            var isMsa = idpClaim == "live.com" || altSecIdClaim?.StartsWith("1:live.com:") == true;
            return isMsa;
        }

        /// <summary>
        /// Method to be called in the event of failed authentication.
        /// </summary>
        /// <param name="context">failure context.</param>
        /// <returns>Task.</returns>
        private static async Task AuthenticationFailedAsync(AuthenticationFailedContext context)
        {
            await Task.CompletedTask;

            var logger = context.HttpContext.GetLogger() ?? new JsonStdoutLogger(new LogValueSet());

            logger
                .AddAuthenticationResultContext(context)
                .FluentAddValue("Exception", context.Exception.Message)
                .LogInfo("jwt_authentication_failed");
        }

        /// <summary>
        /// Builds the callback method to be used after security token is validated.
        /// </summary>
        /// <param name="settings">The RP SaaS settings.</param>
        /// <returns>The callback.</returns>
        private static Func<TokenValidatedContext, Task> BuildOnTokenValidated(RPSaaSSettings settings)
        {
            return async (context) =>
            {
                await Task.CompletedTask;

                var armServicePrincipal = context.Principal;
                var issuerClaim = armServicePrincipal.FindFirstValue("iss");
                var appIdClaim = armServicePrincipal.FindFirstValue("appid");

                var logger = context.HttpContext.GetLogger() ?? new JsonStdoutLogger(new LogValueSet());

                logger
                    .AddAuthenticationResultContext(context)
                    .FluentAddValue("ArmAppId", appIdClaim);

                context.Request.Headers.TryGetValue(RPaaSCorrelationIdHeaderName, out var rpaasCorrelationId);
                logger.FluentAddBaseValue("RPaaSCorrelationId", rpaasCorrelationId);

                if (appIdClaim != settings.AppId)
                {
                    logger.LogError("jwt_appid_notmatched");
                    context.Fail("AppId claim did not match expected claim");
                    return;
                }

                context.HttpContext.Items[SourceArmTokenClaims] = armServicePrincipal;

                ClaimsPrincipal armUserPrincipal;
                var armUserToken = context.HttpContext.Request.Headers[ArmUserTokenHeaderName].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(armUserToken))
                {
                    logger.LogError("jwt_armuser_notprovided");
                    context.Fail("ARM signed user token header not provided");
                    return;
                }

                try
                {
                    armUserPrincipal = ValidateArmUserToken(armUserToken, issuerClaim, logger.NewChildLogger());
                }
                catch (Exception ex)
                {
                    logger.AddErrorDetail(ex.Message).LogError("jwt_armuser_notvalid");
                    context.Fail("Failed to extract ClaimsPrincipal from ARM signed user token header: " + ex.Message);
                    return;
                }

                // Update the current user to the calling user instead of the calling service.
                if (!context.HttpContext.SetUserContextFromClaimsPrincipal(
                    armUserPrincipal, isEmailClaimRequired: false, out string errorMessage))
                {
                    logger.AddErrorDetail(errorMessage).LogError("jwt_armuser_notvalid_claims");
                    context.Fail(errorMessage);
                    return;
                }

                logger.LogInfo("jwt_aadrpsaas_success");
                context.Principal = armUserPrincipal;

                var armUserIdentity = armUserPrincipal.Identities.First();
                await SetCurrentUserIdentityAsync(context, armUserIdentity, logger.NewChildLogger());
            };
        }

        /// <summary>
        /// Validates an additional user token that was supplied in a header for ID linking.
        /// </summary>
        private static async Task SetCurrentUserIdentityAsync(
            TokenValidatedContext context,
            ClaimsIdentity armUserIdentity,
            IDiagnosticsLogger logger)
        {
            var httpContext = context.HttpContext;

            var isMsa = IsArmMsaIdentity(armUserIdentity);

            var userName = armUserIdentity.GetUserEmail(isEmailClaimRequired: false);
            if (string.IsNullOrEmpty(userName))
            {
                if (httpContext.Request.Headers.ContainsKey(CodespaceUserTokenHeaderName))
                {
                    // An additional token was provided for updating the id map,
                    // but id mapping isn't possible without an email claim.
                    logger.LogError("jwt_armuser_missing_email");
                    context.Fail("Cannot update identity map due to missing email claim.");
                }
                else
                {
                    // Allow the request to proceed without any id mapping.
                    logger.LogWarning("jwt_armuser_missing_email");
                }

                return;
            }

            var idMapRepository = httpContext.RequestServices.GetRequiredService<IIdentityMapRepository>();
            var tenantId = armUserIdentity.GetTenantId();
            var idMap = await idMapRepository.GetByUserNameAsync(userName, tenantId, logger.NewChildLogger()) ??
                new IdentityMapEntity(userName, tenantId);

            // Check for a codespace user token header (only valid for MSAs).
            var codespaceUserToken = httpContext.Request.Headers[CodespaceUserTokenHeaderName]
                .FirstOrDefault();
            if (isMsa && !string.IsNullOrWhiteSpace(codespaceUserToken))
            {
                // Authenticate the token.
                ClaimsPrincipal codespaceUserPrincipal;
                try
                {
                    codespaceUserPrincipal = await ValidateAadTokenAsync(codespaceUserToken, httpContext);
                }
                catch (Exception ex)
                {
                    logger.AddErrorDetail(ex.Message).LogError("jwt_user_notvalid");
                    context.Fail("User token authentication failed: " + ex.Message);
                    return;
                }

                // Get the legacy user ID corresponding to this additional authenticated identity.
                var codespaceUserIdentity = codespaceUserPrincipal.Identities.First();
                var codespaceUserId = codespaceUserIdentity.GetLegacyUserId();

                // Add the legacy user ID to the list of linked IDs for the ARM user identity.
                if (idMap.LinkedUserIds?.Contains(codespaceUserId) != true)
                {
                    var linkedIds = new List<string>(idMap.LinkedUserIds ?? Array.Empty<string>());
                    linkedIds.Add(codespaceUserId);
                    await idMapRepository.BackgroundUpdateIfChangedAsync(
                        idMap, null, null, null, linkedIds.ToArray(), logger.NewChildLogger());
                }
            }

            // Set the current user ID set. The profile wasn't fetched, so the user ID set might not
            // include the profile IDs, but it does include any linked IDs.
            var currentUserProvider = httpContext.RequestServices.GetRequiredService<ICurrentUserProvider>();
            var userIdSet = new UserIdSet(
                idMap.CanonicalUserId, idMap.ProfileId, idMap.ProfileProviderId, idMap.LinkedUserIds);
            currentUserProvider.SetUserIds(idMap.Id, userIdSet);
        }

        private static async Task<ClaimsPrincipal> ValidateAadTokenAsync(string token, HttpContext httpContext)
        {
            var validationParameters = AadBearerOptions.TokenValidationParameters.Clone();
            var config = await AadBearerOptions.ConfigurationManager.GetConfigurationAsync(httpContext.RequestAborted);
            if (config != null)
            {
                var issuers = new[] { config.Issuer };
                validationParameters.ValidIssuers = validationParameters.ValidIssuers?.Concat(issuers) ?? issuers;

                validationParameters.IssuerSigningKeys = validationParameters.IssuerSigningKeys?.Concat(config.SigningKeys)
                    ?? config.SigningKeys;
            }

            var validator = new JwtSecurityTokenHandler();
            return validator.ValidateToken(token, validationParameters, out var _);
        }

        private static ClaimsPrincipal ValidateArmUserToken(string token, string tenant, IDiagnosticsLogger logger)
        {
            AddArmUserTenantIfNew(tenant);

            return ArmUserJwtReader.ReadTokenPrincipal(token, logger.NewChildLogger());
        }

        private static void AddArmUserTenantIfNew(string tenant)
        {
            if (ArmUserJwtReader.IssuerCredentials.ContainsKey(tenant))
            {
                return;
            }

            lock (ArmUserJwtReader)
            {
                // Double check existence in case it was added while acquiring the lock
                if (!ArmUserJwtReader.IssuerCredentials.ContainsKey(tenant))
                {
                    ArmUserJwtReader.AddIssuer(tenant, ArmIssuerCredentialCache);
                }
            }
        }

        // Based on the docs here: https://armwiki.azurewebsites.net/authorization/AuthenticateBetweenARMandRP.html?q=certificate
        private class RPSaaSCertifcateCache : JwtCertificateCredentialsHttpCache<RPSaaSCertifcateCache.ArmCertificateSet>
        {
            private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);

            public RPSaaSCertifcateCache(string uri, IDiagnosticsLogger logger)
                : base(new Uri(uri), logger)
            {
                StartPeriodicRefresh(RefreshInterval);
            }

            /// <inheritdoc/>
            protected override Task<IEnumerable<X509Certificate2>> ExtractCertificatesAsync(ArmCertificateSet data)
            {
                var certificates = data.ClientCertificates
                    .Where((cert) => cert.NotBefore < DateTime.UtcNow && cert.NotAfter > DateTime.UtcNow)
                    .OrderByDescending((cert) => cert.NotAfter)
                    .Select((cert) => Convert.FromBase64String(cert.Certificate))
                    .Select((bytes) => new X509Certificate2(bytes));

                return Task.FromResult(certificates);
            }

            [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
            public class ArmCertificateSet
            {
                public IEnumerable<ArmCertificate> ClientCertificates { get; set; }
            }

            [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
            public class ArmCertificate
            {
                public string Thumbprint { get; set; }

                public DateTime NotBefore { get; set; }

                public DateTime NotAfter { get; set; }

                public string Certificate { get; set; }
            }
        }
    }
}
