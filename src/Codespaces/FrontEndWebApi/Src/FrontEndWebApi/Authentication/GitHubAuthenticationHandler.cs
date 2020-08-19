// <copyright file="GitHubAuthenticationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
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
        public const string GitHubAuthenticationHandlerHeader = "__enableGitHubAuth";

        private readonly HttpClient httpClient;
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
        public GitHubAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IGithubApiHttpClientProvider httpClientProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            ITokenProvider tokenProvider,
            IAuthenticationSchemeProvider authSchemeProvider,
            IPlanManager planManager,
            GitHubFixedPlansMapper gitHubFixedPlansMapper)
            : base(options, logger, encoder, clock)
        {
            this.loggerFactory = Requires.NotNull(loggerFactory, nameof(loggerFactory));
            Requires.NotNull(httpClientProvider, nameof(httpClientProvider));
            this.httpClient = httpClientProvider.HttpClient;
            this.jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
            });

            TokenProvider = Requires.NotNull(tokenProvider, nameof(tokenProvider));
            PlanManager = Requires.NotNull(planManager, nameof(PlanManager));
            AuthenticationSchemeProvider = Requires.NotNull(authSchemeProvider, nameof(authSchemeProvider));
            GitHubFixedPlansMapper = Requires.NotNull(gitHubFixedPlansMapper, nameof(gitHubFixedPlansMapper));
        }

        private IAuthenticationSchemeProvider AuthenticationSchemeProvider { get; }

        private ITokenProvider TokenProvider { get; }

        private IPlanManager PlanManager { get; }

        private GitHubFixedPlansMapper GitHubFixedPlansMapper { get; }

        /// <inheritdoc/>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // The majority of this code is copied over from TokenService. The purpose is,
            // to take a GitHub token, authenticate it (sort of) by hitting the GitHub endpoint
            // and getting information from GitHub about the user, then minting our own
            // Cascade token which we then use to genuinely authenticate towards the other calls
            // that we are making.
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
            JObject user;
            try
            {
                var logger = this.loggerFactory.New();
                user = await logger.OperationScopeAsync("github_token_validate", async (innerLogger) =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, "/user");
                    request.Headers.Authorization = new AuthenticationHeaderValue("token", token);
                    var response = await this.httpClient.SendAsync(request);
                    innerLogger.AddValue("HttpResponseStatusCode", response.StatusCode.ToString());

                    response.EnsureSuccessStatusCode();

                    var responseStream = await response.Content.ReadAsStreamAsync();
                    return this.jsonSerializer.Deserialize<JObject>(
                        new JsonTextReader(new StreamReader(responseStream))) !;
                });
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex);
            }

            if (!ReadClaim(user, "login", CustomClaims.Username, out string username))
            {
                return AuthenticateResult.Fail("Missing username claim.");
            }

            var delegatedIdentity = new DelegateIdentity()
            {
                DisplayName = username,
                Id = username,
                Username = username,
            };

            var planToUse = GitHubFixedPlansMapper.GetPlanToUse();
            var plan = await PlanManager.GetAsync(planToUse.Plan, loggerFactory.New());

            var tx = await TokenProvider.GenerateDelegatedVsSaaSTokenAsync(
                plan,
                Partner.GitHub,
                plan.Tenant,
                new[] { "write:environments" },
                delegatedIdentity,
                null,
                null,
                null,
                loggerFactory.New());

            this.Request.Headers["Authorization"] = $"Bearer {tx}";
            this.Request.Headers[GitHubAuthenticationHandlerHeader] = bool.TrueString;

            var authScheme = await this.AuthenticationSchemeProvider.GetSchemeAsync(JwtBearerUtility.VsoAuthenticationScheme);
            var handler = (IAuthenticationHandler)ActivatorUtilities.CreateInstance(
                this.Context.RequestServices, authScheme.HandlerType);

            await handler.InitializeAsync(authScheme, this.Context);

            // Force authentication
            var authenticationResult = await handler.AuthenticateAsync();
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
    }
}