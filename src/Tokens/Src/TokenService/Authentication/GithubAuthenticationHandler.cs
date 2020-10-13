// <copyright file="GithubAuthenticationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
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
            IGithubApiHttpClientProvider httpClientProvider)
            : base(options, logger, encoder, clock)
        {
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

            var logger = Context.RequestServices.GetRequiredService<IDiagnosticsLogger>();
            var token = authHeaderValue.Parameter;
            JObject? user;
            try
            {
                user = await GetGithubUserAsync(token, logger);
            }
            catch (HttpRequestException ex)
            {
                return AuthenticateResult.Fail(ex);
            }

            if (user == null)
            {
                return AuthenticateResult.Fail("Invalid GitHub user token.");
            }

            var claims = new List<Claim>();
            if (!MapClaim(user, "id", CustomClaims.OId, claims))
            {
                return AuthenticateResult.Fail("Missing user id claim.");
            }

            if (!MapClaim(user, "login", CustomClaims.Username, claims))
            {
                return AuthenticateResult.Fail("Missing username claim.");
            }

            string username = user["login"]!.ToString();
            bool authorized;
            try
            {
                authorized = await ValidateGithubAccessAsync(token, username, logger);
            }
            catch (HttpRequestException ex)
            {
                return AuthenticateResult.Fail(ex);
            }

            if (!authorized)
            {
                return AuthenticateResult.Fail(
                    "GitHub user token is not authorized for this application.");
            }

            MapClaim(user, "name", CustomClaims.DisplayName, claims);

            if (!MapClaim(user, "email", CustomClaims.Email, claims))
            {
                // Ensure there is always an email claim by filling in a noreply email.
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

        /// <summary>
        /// Call the GitHub user API to get the user profile information for the token.
        /// </summary>
        private async Task<JObject?> GetGithubUserAsync(string token, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync("github_token_validate", async (innerLogger) =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/user");
                request.Headers.Authorization = new AuthenticationHeaderValue("token", token);
                var response = await this.httpClient.SendAsync(request);
                innerLogger.AddValue("HttpResponseStatusCode", response.StatusCode.ToString());

                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var responseStream = await response.Content.ReadAsStreamAsync();
                return this.jsonSerializer.Deserialize<JObject>(
                    new JsonTextReader(new StreamReader(responseStream))) !;
            });
        }

        /// <summary>
        /// Call the GitHub codespaces API to validate that this token is authorized for access to
        /// codespaces; that is, it was issued to VS / VS Code and has repo scope.
        /// </summary>
        /// <remarks>
        /// This is done even for a token that is only intended to be used with Live Share, in order
        /// to validate that the token was issued to a LS application (VS / VS Code). Otherwise any
        /// third-party app granted access to a user's prublic GitHub profile could also use that
        /// token to access the user's Live Share sessions.
        ///
        /// TODO: LS doesn't actually require repo scope; we may want a different way of authorizing
        /// GitHub tokens for Live Share, eventually.
        /// </remarks>
        private async Task<bool> ValidateGithubAccessAsync(
            string token,
            string username,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync("github_codespaces_validate", async (innerLogger) =>
            {
                // Note this same endpoint works across dev/ppe/prod.
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"/vscs_internal/user/{HttpUtility.UrlEncode(username)}/codespaces");
                request.Headers.Authorization = new AuthenticationHeaderValue("token", token);
                var response = await this.httpClient.SendAsync(request);
                innerLogger.AddValue("HttpResponseStatusCode", response.StatusCode.ToString());

                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return false;
                }

                response.EnsureSuccessStatusCode();

                // There's no need to actually parse the response. Just the fact that this call was
                // successful indicates the token is authorized for codespaces and LS applications.
                return true;
            });
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
