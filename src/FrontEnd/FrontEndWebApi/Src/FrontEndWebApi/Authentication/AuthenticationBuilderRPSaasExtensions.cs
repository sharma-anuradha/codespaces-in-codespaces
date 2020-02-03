﻿// <copyright file="AuthenticationBuilderRPSaasExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
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

        private const string SignedUserHeaderKey = "x-ms-arm-signed-user-token";

        private static readonly string TenantClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

        private static string Issuer { get; } = "https://sts.windows.net/";

        private static IEnumerable<string> DefaultAudiences { get; } = new string[]
        {
            "https://management.core.windows.net/",
        };

        private static IJwtReader SignedUserJwtReader { get; set; } = null;

        private static RPSaaSCertifcateCache SignedUserCertCache { get; set; } = null;

        /// <summary>
        /// Add RPSaaS specific Jwt Bearer.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="authority">The RPSaaS Authority URL.</param>
        /// <returns>the instance of builder.</returns>
        public static AuthenticationBuilder AddRPSaaSJwtBearer(this AuthenticationBuilder builder, string authority)
        {
            builder
                .AddJwtBearer(AuthenticationScheme, options =>
                {
                    var logger = ApplicationServicesProvider.GetRequiredService<IDiagnosticsLogger>();

                    SignedUserJwtReader = new JwtReader();
                    foreach (var audience in DefaultAudiences)
                    {
                        SignedUserJwtReader.AddAudience(audience);
                    }

                    var armTokenReader = new JwtReader();
                    foreach (var audience in DefaultAudiences)
                    {
                        armTokenReader.AddAudience(audience);
                    }

                    var armTokenValidationParameters = armTokenReader.GetValidationParameters(logger, resolveIssuerSigningKeys: false);
                    armTokenValidationParameters.ValidateIssuer = false;

                    options.TokenValidationParameters = armTokenValidationParameters;

                    options.Authority = authority;

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = AuthenticationFailedAsync,
                        OnTokenValidated = OnTokenValidated,
                    };
                })
                .Services
                .AddSingleton<IAsyncWarmup>((serviceProvider) =>
                {
                    var logger = serviceProvider.GetRequiredService<IDiagnosticsLogger>();

                    SignedUserCertCache = new RPSaaSCertifcateCache(logger);

                    return SignedUserCertCache;
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
            await Task.CompletedTask;

            var identity = context.Principal;
            var tenantClaim = identity.Claims.FirstOrDefault(j => j.Type == TenantClaimType);
            var issuerClaim = identity.Claims.FirstOrDefault(i => i.Type == "iss");

            var logger = context.HttpContext.GetLogger() ?? new JsonStdoutLogger(new LogValueSet());

            // Construct the fully tenant qualified issuer url.
            var issuerFull = Issuer + tenantClaim.Value + "/";
            if (issuerClaim.Value != issuerFull)
            {
                // fail request
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
                return;
            }

            context.HttpContext.Items[SourceArmTokenClaims] = identity;

            var signedUserHeader = context.HttpContext.Request.Headers[SignedUserHeaderKey].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(signedUserHeader))
            {
                context.Fail("ARM signed user token header not provided");
                return;
            }

            var claimsPrincipal = TryGetSignedUserClaimsPrincipal(signedUserHeader, issuerFull, logger);
            if (claimsPrincipal == null)
            {
                context.Fail("Failed to extract ClaimsPrincipal from ARM signed user token header");
                return;
            }

            context.HttpContext.User = claimsPrincipal;
        }

        private static ClaimsPrincipal TryGetSignedUserClaimsPrincipal(string token, string tenant, IDiagnosticsLogger logger)
        {
            AddSignedUserTenantIfNew(tenant);

            try
            {
                return SignedUserJwtReader.ReadTokenPrincipal(token, logger);
            }
            catch (SecurityTokenException)
            {
                return null;
            }
        }

        private static void AddSignedUserTenantIfNew(string tenant)
        {
            if (SignedUserJwtReader.IssuerCredentials.ContainsKey(tenant))
            {
                return;
            }

            lock (SignedUserJwtReader)
            {
                // Double check existence in case it was added while acquiring the lock
                if (!SignedUserJwtReader.IssuerCredentials.ContainsKey(tenant))
                {
                    SignedUserJwtReader.AddIssuer(tenant, SignedUserCertCache);
                }
            }
        }

        // Based on the docs here: https://armwiki.azurewebsites.net/authorization/AuthenticateBetweenARMandRP.html?q=certificate
        private class RPSaaSCertifcateCache : JwtCertificateCredentialsHttpCache<RPSaaSCertifcateCache.ArmCertificateSet>
        {
            private static readonly Uri CertificateUri = new Uri("https://management.azure.com:24582/metadata/authentication?api-version=2015-01-01");

            private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);

            public RPSaaSCertifcateCache(IDiagnosticsLogger logger)
                : base(CertificateUri, logger)
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
