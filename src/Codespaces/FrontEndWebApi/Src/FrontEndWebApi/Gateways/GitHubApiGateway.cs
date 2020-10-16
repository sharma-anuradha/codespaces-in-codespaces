// <copyright file="GitHubApiGateway.cs" company="Microsoft">
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Text;
using Kusto.Cloud.Platform.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Gateways
{
    /// <summary>
    /// Provides a simple representation of the GitHub API.
    /// </summary>
    public class GitHubApiGateway
    {
        private const string GithubApiAddress = "https://api.github.com/";
        private const string GithubApiV3MediaType = "application/vnd.github.v3+json";
        private readonly string token;
        private readonly JsonSerializer jsonSerializer;
        private readonly ICurrentLocationProvider currentLocationProvider;
        private readonly IHostEnvironment hostEnvironment;
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubApiGateway"/> class.
        /// </summary>
        public GitHubApiGateway(
            ICurrentLocationProvider currentLocationProvider,
            IHostEnvironment hostEnvironment,
            string token)
        {
            this.currentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            this.hostEnvironment = Requires.NotNull(hostEnvironment, nameof(hostEnvironment));

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    ServiceConstants.ServiceName,
                    typeof(Startup).Assembly.GetName().Version!.ToString()));
            httpClient.BaseAddress = new Uri(GithubApiAddress);
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(GithubApiV3MediaType));

            this.token = token;

            this.jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
            });
        }

        public async Task<JObject> GetUserAsync(IDiagnosticsLogger logger)
        {
            var user = await logger.OperationScopeAsync(
                "github_apigateway_getuser",
                async (logger) =>
                {
                    var request = NewRequest(HttpMethod.Get, "/user");

                    var response = await this.httpClient.SendAsync(request);
                    logger.AddValue("GitHubApiStatusCode", response.StatusCode.ToString());

                    response.EnsureSuccessStatusCode();

                    var responseStream = await response.Content.ReadAsStreamAsync();
                    return this.jsonSerializer.Deserialize<JObject>(
                        new JsonTextReader(new StreamReader(responseStream))) !;
                },
                null,
                true);

            return user;
        }

        public async Task<bool> IsMemberOfMicrosoftOrganisationAsync(
            string username,
            IDiagnosticsLogger logger)
        {
            var result = await logger.OperationScopeAsync(
                "github_apigateway_org_membership",
                async (logger) =>
                {
                    var request = NewRequest(HttpMethod.Get, $"/orgs/microsoft/members/{username}");
                    var response = await this.httpClient.SendAsync(request);
                    return response.StatusCode == HttpStatusCode.NoContent;
                },
                null,
                true);

            return result;
        }

        public async Task<CloudEnvironmentResult> CreateCodespace(
            string username,
            string repoFullName,
            string sku,
            string reference,
            bool forkIfNeeded,
            IDiagnosticsLogger logger)
        {
            // currentUsername string, repoName string, ref string, sku string
            return await logger.OperationScopeAsync(
                "github_apigateway_create_codespace",
                async (logger) =>
                {
                    var request = NewRequest(HttpMethod.Post, $"vscs_internal/user/{username}/codespaces");

                    // get information about the repository
                    var (repositoryId, defaultBranch) = await GetRepositoryDetails(repoFullName, logger);

                    if (repositoryId == 0)
                    {
                        logger.LogError("returned_repository_id_is_0");
                        return null;
                    }

                    // get the location
                    var location = currentLocationProvider.CurrentLocation;

                    var requestBody = new GitHubCodespaceCreateRequest()
                    {
                        RepositoryId = repositoryId,
                        Location = location.ToString(),
                        Sku = sku,
                        Reference = reference ?? defaultBranch,
                        ForkIfNeeded = forkIfNeeded,
                    };

                    // we can let GitHub know we're running in our "special" environments
                    if (hostEnvironment.IsDevelopment())
                    {
                        requestBody.Target = GitHubCodespaceCreateRequest.Targets.Development;
                    }
                    else if (hostEnvironment.IsStaging())
                    {
                        requestBody.Target = GitHubCodespaceCreateRequest.Targets.Staging;
                    }

                    request.Content = new StringContent(
                      JsonConvert.SerializeObject(requestBody),
                      Encoding.UTF8,
                      "application/json");

                    var response = await httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        var responseText = await response.Content.ReadAsStringAsync();
                        logger.AddValue("GitHubApiStatusCode", response.StatusCode.ToString());
                        logger.AddErrorDetail(responseText);
                        logger.LogError("github_creation_failed");
                        return null;
                    }

                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var resultAsJson = this.jsonSerializer.Deserialize<JObject>(
                        new JsonTextReader(new StreamReader(responseStream)));

                    var name = ReadValue<string>(resultAsJson, "name");
                    var state = ReadValue<string>(resultAsJson, "state");

                    var result = new CloudEnvironmentResult()
                    {
                        FriendlyName = name,
                        State = state,
                    };

                    return result;
                },
                null,
                swallowException: false);
        }

        public async Task<bool> VerifyTokenIsValidAsync(
            string username,
            IDiagnosticsLogger logger)
        {
            var list = new List<CloudEnvironmentResult>();
            return await logger.OperationScopeAsync(
                "github_apigateway_validate_token",
                async (logger) =>
                {
                    // While this code hits the same endpoint as <see cref="GetCodespacesAsync"/>
                    // it does not continue to fetch each individual codespace in the results. We
                    // only use this to validate the token (GitHub will reject tokens that aren't
                    // issued for an app we trust.)
                    var request = NewRequest(HttpMethod.Get, $"vscs_internal/user/{username}/codespaces");
                    var response = await httpClient.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        var responseText = await response.Content.ReadAsStringAsync();
                        logger.AddValue("GitHubApiStatusCode", response.StatusCode.ToString());
                        logger.AddErrorDetail(responseText)
                              .LogWarning("github_invalid_token_used");

                        return false;
                    }

                    return true;
                });
        }

        public async Task<IEnumerable<CloudEnvironmentResult>> GetCodespacesAsync(
            string username,
            IDiagnosticsLogger logger)
        {
            // Note: this operation is very inefficient, but we decided to go for it in this form
            // because, for the time being, this will be *good enough*. In reality, we'd much rather
            // go and ask the database for this information, but we want to avoid doing additional
            // checks etc., that GitHub is already doing
            var list = new List<CloudEnvironmentResult>();
            return await logger.OperationScopeAsync(
                "github_apigateway_list_codespaces",
                async (logger) =>
                {
                    var request = NewRequest(HttpMethod.Get, $"vscs_internal/user/{username}/codespaces");

                    var response = await httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        var responseText = await response.Content.ReadAsStringAsync();
                        logger.AddErrorDetail(responseText);
                        logger.LogError("github_list_codespaces_failed");
                        return list;
                    }

                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var resultAsJson = this.jsonSerializer.Deserialize<JObject>(
                        new JsonTextReader(new StreamReader(responseStream)));

                    if (!resultAsJson.TryGetValue("codespaces", out var codespaces))
                    {
                        return list;
                    }

                    foreach (var codespace in codespaces.ToArrayIfNotAlready().Where(x => x != null))
                    {
                        try
                        {
                            var urlValue = codespace.Value<string>("url");
                            if (string.IsNullOrEmpty(urlValue))
                            {
                                continue;
                            }

                            var cs = await GetCodespaceAsync(urlValue, logger);
                            if (cs == null)
                            {
                                continue;
                            }

                            list.Add(cs);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    return list;
                },
                null,
                swallowException: true);
        }

        public async Task<IActionResult> DeleteCodespaceAsync(
            string username,
            string friendlyName,
            IDiagnosticsLogger logger)
        {
            var result = await logger.OperationScopeAsync("github_apigateway_delete_codespace", async (logger) =>
            {
                var request = NewRequest(HttpMethod.Delete, $"vscs_internal/user/{username}/codespaces/{friendlyName}");
                var response = await this.httpClient.SendAsync(request);

                logger.AddValue("GitHubApiStatusCode", response.StatusCode.ToString());

                return new StatusCodeResult((int)response.StatusCode);
            });

            return result;
        }

        public async Task<CloudEnvironmentResult> ResumeCodespaceAsync(
            Guid codespaceId,
            IDiagnosticsLogger logger)
        {
            var result = await logger.OperationScopeAsync("github_apigateway_resume_codespace", async (logger) =>
            {
                var request = NewRequest(HttpMethod.Post, $"vscs_internal/proxy/environments/{codespaceId.ToString()}/start");
                var response = await this.httpClient.SendAsync(request);

                logger.AddValue("GitHubApiStatusCode", response.StatusCode.ToString());

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseStream = await response.Content.ReadAsStreamAsync();
                var result = this.jsonSerializer.Deserialize<CloudEnvironmentResult>(
                    new JsonTextReader(new StreamReader(responseStream)));

                return result;
            });

            return result;
        }

        public async Task<CloudEnvironmentResult> GetCodespaceAsync(
            string username,
            string friendlyName,
            IDiagnosticsLogger logger)
        {
            var result = await logger.OperationScopeAsync(
                "github_apigateway_get_codespace_from_url",
                async (logger) =>
                {
                    var request = NewRequest(HttpMethod.Get, $"vscs_internal/user/{username}/codespaces/{friendlyName}");
                    var response = await this.httpClient.SendAsync(request);

                    logger.AddValue("GitHubApiStatusCode", response.StatusCode.ToString());

                    response.EnsureSuccessStatusCode();

                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var repo = this.jsonSerializer.Deserialize<JObject>(
                        new JsonTextReader(new StreamReader(responseStream)));

                    // try reading the GitHub state, and see if it's provisioned
                    if (string.Equals(ReadValue<string>(repo, "state"), "provisioned", StringComparison.OrdinalIgnoreCase))
                    {
                        var environment = repo.Value<JObject>("environment");
                        return environment.ToObject<CloudEnvironmentResult>();
                    }

                    return null;
                },
                null,
                swallowException: true);

            return result;
        }

        public async Task<CloudEnvironmentResult> GetCloudEnvironmentResultById(
            string username,
            Guid environmentId,
            IDiagnosticsLogger logger)
        {
            var list = await this.GetCodespacesAsync(username, logger);
            var element = list.FirstOrDefault(x => x.Id == environmentId.ToString());

            return element;
        }

        private async Task<CloudEnvironmentResult> GetCodespaceAsync(
            string apiUrl,
            IDiagnosticsLogger logger)
        {
            var result = await logger.OperationScopeAsync(
                "github_apigateway_get_codespace_from_url",
                async (logger) =>
                {
                    var request = NewRequest(HttpMethod.Get, apiUrl);
                    var response = await this.httpClient.SendAsync(request);

                    logger.AddValue("GitHubApiStatusCode", response.StatusCode.ToString());

                    response.EnsureSuccessStatusCode();

                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var repo = this.jsonSerializer.Deserialize<JObject>(
                        new JsonTextReader(new StreamReader(responseStream)));

                    var environment = repo.Value<JObject>("environment");
                    return environment.ToObject<CloudEnvironmentResult>();
                },
                null,
                swallowException: true);

            return result;
        }

        private async Task<(int repositoryId, string defaultBranch)> GetRepositoryDetails(
          string repoFullName,
          IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(repoFullName, nameof(repoFullName));
            var (owner, repoName) = GetRepositoryFromFullName(repoFullName, logger);

            var result = await logger.OperationScopeAsync(
                "github_apigateway_repository_details",
                async (logger) =>
                {
                    var request = NewRequest(HttpMethod.Get, $"/repos/{owner}/{repoName}");
                    var response = await this.httpClient.SendAsync(request);

                    logger.AddValue("GitHubApiStatusCode", response.StatusCode.ToString());

                    response.EnsureSuccessStatusCode();

                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var repo = this.jsonSerializer.Deserialize<JObject>(
                        new JsonTextReader(new StreamReader(responseStream))) !;

                    var id = ReadValue<int>(repo, "id");
                    var defaultBranch = ReadValue<string>(repo, "default_branch");

                    return (id, defaultBranch);
                },
                null,
                swallowException: true);

            return result;
        }

        private (string owner, string repoName) GetRepositoryFromFullName(
            string repoFullName,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(repoFullName, nameof(repoFullName));

            var parts = repoFullName.Split('/');
            if (parts.Length != 2
                || string.IsNullOrWhiteSpace(parts[0])
                || string.IsNullOrWhiteSpace(parts[1]))
            {
                throw new ArgumentException($"The Repository Full Name ({repoFullName}) was in an incorrect format.");
            }

            return (parts[0], parts[1]);
        }

        private static T ReadValue<T>(JObject obj, string propertyName)
        {
            if (obj.TryGetValue(propertyName, out JToken token)
                && (token!.Type == JTokenType.String || token!.Type == JTokenType.Integer))
            {
                return token.Value<T>();
            }

            return default;
        }

        private HttpRequestMessage NewRequest(
            HttpMethod method,
            string path)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("token", token);

            return request;
        }

        public static bool GetGitHubUsername(ClaimsPrincipal user, out string username)
        {
            username = user.FindFirst(CustomClaims.Username)?.Value;
            return !string.IsNullOrWhiteSpace(username);
        }

        public static bool IsGitHubRepository(string seedMoniker, out string repository)
        {
            var pattern = @"(?:http(?:s)?:\/\/)(?:www.)?github.com\/([-a-zA-Z0-9@:%_\-\+~#?&//=]*)";

            repository = null;
            if (string.IsNullOrEmpty(seedMoniker))
            {
                return false;
            }

            var m = Regex.Match(seedMoniker, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            if (!m.Success)
            {
                return false;
            }

            repository = m.Groups[1].Value;
            return true;
        }
    }
}