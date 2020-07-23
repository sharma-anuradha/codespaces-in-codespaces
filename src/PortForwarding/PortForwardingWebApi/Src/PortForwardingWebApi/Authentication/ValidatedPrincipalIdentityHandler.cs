// <copyright file="ValidatedPrincipalIdentityHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Authentication
{
    /// <inheritdoc/>
    public class ValidatedPrincipalIdentityHandler : IValidatedPrincipalIdentityHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidatedPrincipalIdentityHandler"/> class.
        /// </summary>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="httpContextAccessor">The http context acessor.</param>
        /// <param name="hostingEnvironment">The aspnetcore hosting environment.</param>
        public ValidatedPrincipalIdentityHandler(
            ICurrentUserProvider currentUserProvider,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment hostingEnvironment)
        {
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
            HostingEnvironment = Requires.NotNull(hostingEnvironment, nameof(hostingEnvironment));
            HttpContextAccessor = Requires.NotNull(httpContextAccessor, nameof(httpContextAccessor));
        }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private IHttpContextAccessor HttpContextAccessor { get; }

        private IWebHostEnvironment HostingEnvironment { get; }

        /// <inheritdoc/>
        public Task<ClaimsPrincipal> ValidatedPrincipalAsync(ClaimsPrincipal principal, JwtSecurityToken jwtToken, IDiagnosticsLogger logger)
        {
            Requires.NotNull(principal, nameof(principal));

            var identity = principal.Identities.First();
            var httpContext = HttpContextAccessor.HttpContext;

            // Setup debug output if in debug
            if (System.Diagnostics.Debugger.IsAttached)
            {
                var debug = new System.Text.StringBuilder($"\nValidated Principal {identity.Name}\n");
                foreach (var claim in identity.Claims.OrderBy(c => c.Type))
                {
                    debug.AppendLine($"{claim.Type}: {claim.Value}");
                }

                debug.AppendLine();
                Console.WriteLine(debug.ToString());
            }

            // Build principal and identity
            var vsoClaimsIdentity = new VsoClaimsIdentity(identity);
            var newClaimsPrincipal = new ClaimsPrincipal(vsoClaimsIdentity);

            SetCurrentUserBearerToken(httpContext, jwtToken, logger);

            DebugWriteIdentityInfoAsync(identity);
            return Task.FromResult(newClaimsPrincipal);
        }

        private void SetCurrentUserBearerToken(HttpContext httpContext, JwtSecurityToken jwtToken, IDiagnosticsLogger logger)
        {
            void ThrowForFailure(string message)
            {
                logger.LogErrorWithDetail("validated_principal_set_token_error", message);
                throw new IdentityValidationException(message);
            }

            // JWT Bearer provides the token, Cookie does not.
            // The bearer token must be set in order to read the user profile.
            // Note that this bearer token is encrypted and Live Share must be able to decrypt it.
            if (jwtToken != null)
            {
                var bearerToken = jwtToken.RawData;

                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    ThrowForFailure("No JWT bearer token");
                }

                CurrentUserProvider.SetBearerToken(jwtToken.RawData);
            }
        }

        private void DebugWriteIdentityInfoAsync(ClaimsIdentity identity)
        {
            // Emit token diagnostics in Development mode.
            // This emits PII to k8s logs, but these will not get picked up by the fluentd
            // and send to Geneva because the lines are intentionally not in JSON format.
            if (HostingEnvironment.IsDevelopment())
            {
                void PiiWriteline(string name, object value)
                {
                    const string prefix = "validated_principal_info";
                    if (value is string str)
                    {
                        value = "\"" + value + "\"";
                    }
                    else if (value is bool b)
                    {
                        value = b.ToString().ToLowerInvariant();
                    }
                    else if (value is null)
                    {
                        value = "null";
                    }

                    Console.WriteLine($"{prefix}: {name.PadLeft(50)}: {value}");
                }

                var httpContext = HttpContextAccessor.HttpContext;
                var httpContext_GetCurrentUserId = httpContext.GetCurrentUserId();
                var httpContext_GetCurrentUserProfileId = httpContext.GetCurrentUserProfileId();
                var httpContext_GetCurrentUserProfileProviderId = httpContext.GetCurrentUserProfileProviderId();
                var httpContext_GetCurrentUserCanonicalUserId = httpContext.GetCurrentUserCanonicalUserId();
                var httpContext_GetCurrentUserEmail = httpContext.GetCurrentUserEmail();
                var httpContext_GetCurrentUserDisplayName = httpContext.GetCurrentUserDisplayName();
                var httpContext_GetCurrentUserPreferredUserName = httpContext.GetCurrentUserPreferredUserName();
                var identity_GetClientAppId = identity.GetClientAppid();
                var identity_GetAltSecId = identity.GetAltSecId();
                var identity_GetPuid = identity.GetPuid();
                var identity_GetObjectId = identity.GetObjectId();
                var identity_GetTenantId = identity.GetTenantId();
                var identity_GetUserEmail = identity.GetUserEmail(false);
                var identity_GetUserDisplayName = identity.GetUserDisplayName();

                PiiWriteline(nameof(httpContext_GetCurrentUserDisplayName), httpContext_GetCurrentUserDisplayName);
                PiiWriteline(nameof(httpContext_GetCurrentUserEmail), httpContext_GetCurrentUserEmail);
                PiiWriteline(nameof(httpContext_GetCurrentUserId), httpContext_GetCurrentUserId);
                PiiWriteline(nameof(httpContext_GetCurrentUserCanonicalUserId), httpContext_GetCurrentUserCanonicalUserId);
                PiiWriteline(nameof(httpContext_GetCurrentUserProfileId), httpContext_GetCurrentUserProfileId);
                PiiWriteline(nameof(httpContext_GetCurrentUserProfileProviderId), httpContext_GetCurrentUserProfileProviderId);
                PiiWriteline(nameof(httpContext_GetCurrentUserPreferredUserName), httpContext_GetCurrentUserPreferredUserName);
                PiiWriteline(nameof(identity_GetClientAppId), identity_GetClientAppId);
                PiiWriteline(nameof(identity_GetAltSecId), identity_GetAltSecId);
                PiiWriteline(nameof(identity_GetPuid), identity_GetPuid);
                PiiWriteline(nameof(identity_GetObjectId), identity_GetObjectId);
                PiiWriteline(nameof(identity_GetTenantId), identity_GetTenantId);
                PiiWriteline(nameof(identity_GetUserDisplayName), identity_GetUserDisplayName);
                PiiWriteline(nameof(identity_GetUserEmail), identity_GetUserEmail);
            }
        }
    }
}
