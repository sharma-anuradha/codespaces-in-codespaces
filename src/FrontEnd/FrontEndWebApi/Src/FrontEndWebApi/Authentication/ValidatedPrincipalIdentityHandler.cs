// <copyright file="ValidatedPrincipalIdentityHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.IdentityMap;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Contracts;
using CommonAuthenticationConstants = Microsoft.VsSaaS.Common.Identity.AuthenticationConstants;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <inheritdoc/>
    public class ValidatedPrincipalIdentityHandler : IValidatedPrincipalIdentityHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidatedPrincipalIdentityHandler"/> class.
        /// </summary>
        /// <param name="identityMapRepository">The identity map repository.</param>
        /// <param name="profileRepository">The profile repository.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="httpContextAccessor">The http context acessor.</param>
        /// <param name="hostingEnvironment">The aspnetcore hosting environment.</param>
        /// <param name="diagnosticsLoggerFactory">The logger factory.</param>
        /// <param name="logValues">The default log values.</param>
        public ValidatedPrincipalIdentityHandler(
            IIdentityMapRepository identityMapRepository,
            IProfileRepository profileRepository,
            ICurrentUserProvider currentUserProvider,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment hostingEnvironment,
            IDiagnosticsLoggerFactory diagnosticsLoggerFactory,
            LogValueSet logValues)
        {
            IdentityMapRepository = Requires.NotNull(identityMapRepository, nameof(identityMapRepository));
            ProfileRepository = Requires.NotNull(profileRepository, nameof(profileRepository));
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
            HostingEnvironment = Requires.NotNull(hostingEnvironment, nameof(hostingEnvironment));
            HttpContextAccessor = Requires.NotNull(httpContextAccessor, nameof(httpContextAccessor));
            DiagnosticsLoggerFactory = Requires.NotNull(diagnosticsLoggerFactory, nameof(diagnosticsLoggerFactory));
            LogValues = Requires.NotNull(logValues, nameof(logValues));
        }

        private IIdentityMapRepository IdentityMapRepository { get; }

        private IProfileRepository ProfileRepository { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private IHttpContextAccessor HttpContextAccessor { get; }

        private IWebHostEnvironment HostingEnvironment { get; }

        private IDiagnosticsLoggerFactory DiagnosticsLoggerFactory { get; }

        private LogValueSet LogValues { get; }

        /// <inheritdoc/>
        public async Task<ClaimsPrincipal> ValidatedPrincipalAsync(ClaimsPrincipal principal, JwtSecurityToken jwtToken)
        {
            Requires.NotNull(principal, nameof(principal));

            var identity = principal.Identities.First();
            var httpContext = HttpContextAccessor.HttpContext;
            var logger = httpContext.GetLogger()?.NewChildLogger() ?? DiagnosticsLoggerFactory.New(LogValues);

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

            // Change the current user
            // CurrentUserProvider.SetPrincipal(vsoClaimsPrincipal);

            // Handle the Live Share profile. We always do this for now.
            // In the future, we can eliminate calls to profile during authentication,
            // after we no longer use profile id or provider id, but only canonical user id.
            var profile = await GetProfileAsync(httpContext, jwtToken, logger);
            CurrentUserProvider.SetProfile(profile);

            // Get the mapping between canonical user id and profile id and save it for the current user context.
            var map = await GetUserIdentityMap(identity, profile, logger);
            CurrentUserProvider.SetUserIds(map.Id, map.CanonicalUserId, map.ProfileId, map.ProfileProviderId);

            // Always set the current user id to the cannoncal user id if we have one.
            var currentUserId = map.CanonicalUserId ?? map.ProfileId;
            httpContext.SetCurrentUserId(currentUserId);

            DebugWriteIdentityInfo(identity);

            return newClaimsPrincipal;
        }

        private async Task<IIdentityMapEntity> GetUserIdentityMap(ClaimsIdentity identity, Profile profile, IDiagnosticsLogger logger)
        {
            var useCanonicalUserId = identity.SupportsCanonicalUserId();
            var isMsa = identity.IsMsaIdentity();
            var userName = identity.GetUserEmail(true);
            var tenantId = isMsa ? CommonAuthenticationConstants.MsaPseudoTenantId : identity.GetTenantId();
            var useProfile = !isMsa || !useCanonicalUserId;

            var map = await IdentityMapRepository.GetByUserNameAsync(userName, tenantId, logger) ?? new IdentityMapEntity(userName, tenantId);

            // Update the map if any one is incomplete.
            if (map.CanonicalUserId == null ||
                map.ProfileId == null ||
                map.ProfileProviderId == null)
            {
                var updateCanonicalUserId = default(string);
                var updateProfileId = default(string);
                var updateProfileProviderId = default(string);

                // The profile id values are backwards compatible when we're not in "canonical" mode.
                // Live Share may not return the same profile for old/compatible and new audience tokens.
                if (map.ProfileId == null && useProfile)
                {
                    updateProfileId = profile?.Id;
                }

                if (map.ProfileProviderId == null && useProfile)
                {
                    updateProfileProviderId = profile?.ProviderId;
                }

                if (map.CanonicalUserId == null && useCanonicalUserId)
                {
                    updateCanonicalUserId = identity.GetCanonicalUserId();
                }

                map = await IdentityMapRepository.BackgroundUpdateIfChangedAsync(map, updateCanonicalUserId, updateProfileId, updateProfileProviderId, logger);
            }

            return map;
        }

        private async Task<Profile> GetProfileAsync(HttpContext httpContext, JwtSecurityToken jwtToken, IDiagnosticsLogger logger)
        {
            Profile Fail(string message)
            {
                CurrentUserProvider.SetBearerToken(null);
                logger.LogErrorWithDetail("validated_principal_get_profile_error", message);
                throw new IdentityValidationException(message);
            }

            // JWT Bearer provides the token, Cookie does not.
            // The bearer token must be set in order to read the user profile.
            // Note that this bearer token is encrypted and Live Share must be able to decrypt it.
            if (jwtToken != null)
            {
                var bearerToken = jwtToken.RawData;

                if (string.IsNullOrEmpty(bearerToken))
                {
                    return Fail("No JWT bearer token");
                }

                CurrentUserProvider.SetBearerToken(jwtToken.RawData);
            }

            try
            {
                var profile = await ProfileRepository.GetCurrentUserProfileAsync(logger.NewChildLogger());
                if (profile is null)
                {
                    return Fail("Could not get Live Share profile: null");
                }

                return profile;
            }
            catch (Exception ex)
            {
                return Fail($"Could not get Live Share profile: {ex.Message}");
            }
        }

        private void DebugWriteIdentityInfo(ClaimsIdentity identity)
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
                var currentUserProvider = CurrentUserProvider;
                var currentUserProvider_GetProfile_Id = currentUserProvider.Profile?.Id;
                var currentUserProvider_GetProfile_ProviderId = currentUserProvider.Profile?.ProviderId;
                var currentUserProvider_GetProfile_Name = currentUserProvider.Profile?.Name;
                var currentUserProvider_GetProfile_Email = currentUserProvider.Profile?.Email;
                var currentUserProvider_GetProfile_UserName = currentUserProvider.Profile?.UserName;
                var identity_GetClientAppId = identity.GetClientAppid();
                var identity_GetAltSecId = identity.GetAltSecId();
                var identity_GetPuid = identity.GetPuid();
                var identity_GetObjectId = identity.GetObjectId();
                var identity_GetTenantId = identity.GetTenantId();
                var identity_GetUserEmail = identity.GetUserEmail(false);
                var identity_GetUserDisplayName = identity.GetUserDisplayName();

                PiiWriteline(nameof(currentUserProvider_GetProfile_Email), currentUserProvider_GetProfile_Email);
                PiiWriteline(nameof(currentUserProvider_GetProfile_Id), currentUserProvider_GetProfile_Id);
                PiiWriteline(nameof(currentUserProvider_GetProfile_Name), currentUserProvider_GetProfile_Name);
                PiiWriteline(nameof(currentUserProvider_GetProfile_ProviderId), currentUserProvider_GetProfile_ProviderId);
                PiiWriteline(nameof(currentUserProvider_GetProfile_ProviderId), currentUserProvider_GetProfile_ProviderId);
                PiiWriteline(nameof(currentUserProvider_GetProfile_UserName), currentUserProvider_GetProfile_UserName);
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
