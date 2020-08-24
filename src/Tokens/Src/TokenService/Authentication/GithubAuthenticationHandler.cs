// <copyright file="GithubAuthenticationHandler.cs" company="Microsoft">
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.TokenService.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.TokenService.Authentication
{
    /// <summary>
    /// Authenticates users by validating tokens via the GitHub API.
    /// </summary>
    public class GithubAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly HttpClient httpClient;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly JsonSerializer jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GithubAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="options">Authentication options.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="encoder">URL encoder.</param>
        /// <param name="clock">System clock.</param>
        /// <param name="httpClientProvider">HTTP client provider.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        public GithubAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IGithubApiHttpClientProvider httpClientProvider,
            IDiagnosticsLoggerFactory loggerFactory)
            : base(options, logger, encoder, clock)
        {
            this.loggerFactory = Requires.NotNull(loggerFactory, nameof(loggerFactory));
            Requires.NotNull(httpClientProvider, nameof(httpClientProvider));
            this.httpClient = httpClientProvider.HttpClient;
            this.jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
            });
        }

        /// <inheritdoc/>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
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

            var claims = new List<Claim>();
            if (!MapClaim(user, "id", CustomClaims.OId, claims))
            {
                return AuthenticateResult.Fail("Missing user id claim.");
            }

            MapClaim(user, "name", CustomClaims.DisplayName, claims);

            if (MapClaim(user, "login", CustomClaims.Username, claims) &&
                !MapClaim(user, "email", CustomClaims.Email, claims))
            {
                // Ensure there is always an email claim by filling in a noreply email.
                string username = user["login"] !.ToString();
                claims.Add(new Claim(CustomClaims.Email, $"{username}@users.noreply.github.com"));
            }

            // Add a tenant ID claim to the identity, as some other code might expect it.
            // Note this ID is just "github" rather than a GUID like most tenant IDs.
            claims.Add(new Claim(CustomClaims.TenantId, ProviderNames.GitHub));
            claims.Add(new Claim(CustomClaims.Provider, ProviderNames.GitHub));

            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "AuthenticationTypes.Federation",
                nameType: CustomClaims.DisplayName,
                roleType: null);
            return AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(identity), Scheme.Name));
        }

        private static bool MapClaim(
            JObject user,
            string propertyName,
            string claimType,
            ICollection<Claim> claims)
        {
            if (user.TryGetValue(propertyName, out JToken? value) &&
                (value!.Type == JTokenType.String || value!.Type == JTokenType.Integer))
            {
                claims.Add(new Claim(claimType, value.ToString()));
                return true;
            }

            return false;
        }
    }
}
