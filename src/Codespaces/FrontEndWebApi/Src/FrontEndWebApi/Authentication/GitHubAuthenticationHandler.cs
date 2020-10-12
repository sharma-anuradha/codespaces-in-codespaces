// <copyright file="GitHubAuthenticationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Gateways;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Providers;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// Authenticates users by validating tokens via the GitHub API.
    /// </summary>
    public class GitHubAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>
        /// Defines the header value set by this auth handler when ran, so that we can check
        /// later down the line.
        /// </summary>
        public const string GitHubAuthenticationHandlerHeader = "__githubToken";

        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly JsonSerializer jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="options">Authentication options.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="encoder">URL encoder.</param>
        /// <param name="clock">System clock.</param>
        /// <param name="httpClientProvider">HTTP client provider.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="tokenProvider">The Token Provider.</param>
        /// <param name="authSchemeProvider">The Auth Scheme Provider.</param>
        /// <param name="planManager">The Plan Manager.</param>
        /// <param name="gitHubFixedPlansMapper">The GitHub Plans Mapper.</param>
        /// <param name="currentUserProvider">The Current User Provider.</param>
        public GitHubAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,            
            IDiagnosticsLoggerFactory loggerFactory,
            ITokenProvider tokenProvider,
            IAuthenticationSchemeProvider authSchemeProvider,
            IPlanManager planManager,
            IHostEnvironment hostEnvironment,
            GitHubFixedPlansMapper gitHubFixedPlansMapper,
            ICurrentUserProvider currentUserProvider,
            ICurrentLocationProvider currentLocationProvider)
            : base(options, logger, encoder, clock)
        {
            this.loggerFactory = Requires.NotNull(loggerFactory, nameof(loggerFactory));
            this.jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
            });

            TokenProvider = Requires.NotNull(tokenProvider, nameof(tokenProvider));
            PlanManager = Requires.NotNull(planManager, nameof(PlanManager));
            AuthenticationSchemeProvider = Requires.NotNull(authSchemeProvider, nameof(authSchemeProvider));
            GitHubFixedPlansMapper = Requires.NotNull(gitHubFixedPlansMapper, nameof(gitHubFixedPlansMapper));
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
            CurrentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            HostEnvironment = Requires.NotNull(hostEnvironment, nameof(hostEnvironment));
        }

        private IAuthenticationSchemeProvider AuthenticationSchemeProvider { get; }

        private ITokenProvider TokenProvider { get; }

        private IPlanManager PlanManager { get; }

        private GitHubFixedPlansMapper GitHubFixedPlansMapper { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private ICurrentLocationProvider CurrentLocationProvider { get; }

        private IHostEnvironment HostEnvironment { get; }

        /// <inheritdoc/>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // The majority of this code is copied over from TokenService. The purpose is,
            // to take a GitHub token, authenticate it (sort of) by hitting the GitHub endpoint
            // and getting information from GitHub about the user, then minting our own
            // Cascade token which we then use to genuinely authenticate towards the other calls
            // that we are making.
            var logger = loggerFactory.New();

            var authHeader = Request.Headers[HeaderNames.Authorization].ToString();
            if (!AuthenticationHeaderValue.TryParse(authHeader, out var authHeaderValue))
            {
                return AuthenticateResult.NoResult();
            }

            if (!string.Equals(authHeaderValue.Scheme, Scheme.Name, StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.NoResult();
            }

            var token = authHeaderValue.Parameter;

            // we do this, because we provide the token in a different way
            var gateway = new GitHubApiGateway(CurrentLocationProvider, HostEnvironment, token);

            JObject user;
            try
            {
                user = await gateway.GetUserAsync(logger);
                if (user == null)
                {
                    return AuthenticateResult.Fail("User returned from endpoint is null.");
                }
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex);
            }

            if (!ReadClaim(user, "login", CustomClaims.Username, out string username))
            {
                return AuthenticateResult.Fail("Missing username claim.");
            }

            if (!ReadClaim(user, CustomClaims.DisplayName, CustomClaims.DisplayName, out string displayName))
            {
                displayName = username;
            }

            if (!ReadClaim(user, CustomClaims.Id, CustomClaims.Id, out string id))
            {
                return AuthenticateResult.Fail("Missing id claim.");
            }

            // Verify the token with GitHub
            if (!await gateway.VerifyTokenIsValidAsync(username, logger))
            {
                return AuthenticateResult.Fail("GitHub Token is invalid or the app it was issued to is not trusted.");
            }

            bool isMicrosoftInternalUser = await gateway.IsMemberOfMicrosoftOrganisationAsync(username, logger);
           
            var delegatedIdentity = new DelegateIdentity()
            {
                DisplayName = displayName,
                Id = id,
                Username = username,
            };

            var planToUse = GitHubFixedPlansMapper.GetPlanToUse();
            var plan = await PlanManager.GetAsync(planToUse.Plan, loggerFactory.New());

            var tx = await TokenProvider.GenerateDelegatedVsSaaSTokenAsync(
                plan,
                Partner.GitHub,
                "github",   // TODO: janraj const
                new[] { "write:environments" },
                delegatedIdentity,
                null,
                null,
                null,
                loggerFactory.New());

            this.Request.Headers["Authorization"] = $"Bearer {tx}";
            this.Request.Headers[GitHubAuthenticationHandlerHeader] = token;

            var authScheme = await this.AuthenticationSchemeProvider.GetSchemeAsync(JwtBearerUtility.VsoAuthenticationScheme);
            var handler = (IAuthenticationHandler)ActivatorUtilities.CreateInstance(
                this.Context.RequestServices, authScheme.HandlerType);

            await handler.InitializeAsync(authScheme, this.Context);

            // Force authentication
            var authenticationResult = await handler.AuthenticateAsync();
            var profile = await CurrentUserProvider.GetProfileAsync();

            // We do this, because users that we "fake", from GitHub, don't carry the same information
            // about SKU access. This makes it easier to keep the existing logic for checking SKU access.
            if (profile != null && isMicrosoftInternalUser) 
            {
                if (!profile.GetProgramsItem<bool>(ProfileExtensions.VisualStudioOnlineInternalWindowsSkuUserProgram))
                {
                    if (profile.Programs == null)
                    {
                        profile.Programs = new Dictionary<string, object>();
                    }

                    profile.Programs.AddOrSet(ProfileExtensions.VisualStudioOnlineInternalWindowsSkuUserProgram, true);
                }
            }

            return authenticationResult;
        }

        private static bool ReadClaim(
            JObject user,
            string propertyName,
            string claimType,
            out string claimValue)
        {
            claimValue = null;
#nullable enable
            if (user.TryGetValue(propertyName, out JToken? value) &&
                (value!.Type == JTokenType.String || value!.Type == JTokenType.Integer))
            {
                claimValue = value.ToString();

                return true;
            }
#nullable restore
            return false;
        }

        public static bool IsInGitHubAuthenticatedSession(HttpRequest request, out string token)
        {
            // HOTFIX for release 10/5 weekly release - disable GitHub API forking
            // To remove: delete these 2 lines and uncomment the below code
            token = null;
            return false;

            /*
            token = null;
            request.Headers.TryGetValue(
                GitHubAuthenticationHandler.GitHubAuthenticationHandlerHeader,
                out StringValues headerValue);

            if (headerValue.All(x => string.IsNullOrEmpty(x)))
            {
                // it appears we aren't in a GitHub auth session
                return false;
            }

            token = headerValue.FirstOrDefault();
            return true;
            */
        }
    }
}