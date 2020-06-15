// <copyright file="HttpClientWorkspaceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace
{
    /// <inheritdoc/>
    public class HttpClientWorkspaceRepository : IWorkspaceRepository
    {
        private const string LogBaseName = "httpclientworkspacerepository";
        private const string Path = "workspace";
        private const string AuthHeaderName = "Authorization";
        private const string AuthTokenPrefix = "Bearer ";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientWorkspaceRepository"/> class.
        /// </summary>
        /// <param name="httpClientProvider">The http client provider.</param>
        public HttpClientWorkspaceRepository(
            IHttpClientProvider<WorkspaceHttpClientProviderOptions> httpClientProvider)
        {
            HttpClientProvider = httpClientProvider;
        }

        private IHttpClientProvider HttpClientProvider { get; }

        /// <inheritdoc/>
        public Task<WorkspaceResponse> CreateAsync(WorkspaceRequest workspace, string authToken, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_create",
                async (childLogger) =>
                {
                    var payload = JsonConvert.SerializeObject(workspace);
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");

                    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, Path);
                    httpRequestMessage.Headers.Add("Accept", "application/json");
                    if (!string.IsNullOrEmpty(authToken))
                    {
                        httpRequestMessage.Headers.Add(AuthHeaderName, AuthTokenPrefix + authToken);
                    }

                    httpRequestMessage.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                    var response = await HttpClientProvider.HttpClient.SendAsync(httpRequestMessage);
                    logger.AddClientHttpResponseDetails(response);

                    await response.ThrowIfFailedAsync();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var workspaceResponse = JsonConvert.DeserializeObject<WorkspaceResponse>(responseContent);
                    return workspaceResponse;
                });
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string workspaceId, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete",
                async (childLogger) =>
                {
                    var response = await HttpClientProvider.HttpClient.DeleteAsync($"{Path}/{workspaceId}");
                    logger.AddClientHttpResponseDetails(response);

                    // The workspace may already been cleaned up. Ignore not-found response.
                    if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        await response.ThrowIfFailedAsync();
                    }
                });
        }

        /// <inheritdoc/>
        public Task<string> GetInvitationLinkAsync(SharedInvitationLinkInfo invitationLinkInfo, string authToken, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_create_invitation_id",
                async (childLogger) =>
                {
                    var payload = JsonConvert.SerializeObject(invitationLinkInfo);
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");

                    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, InvitationLinkPath(invitationLinkInfo.WorkspaceId));
                    httpRequestMessage.Headers.Add("Accept", "application/json");
                    if (!string.IsNullOrEmpty(authToken))
                    {
                        httpRequestMessage.Headers.Add(AuthHeaderName, AuthTokenPrefix + authToken);
                    }

                    httpRequestMessage.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                    var response = await HttpClientProvider.HttpClient.SendAsync(httpRequestMessage);
                    logger.AddClientHttpResponseDetails(response);

                    await response.ThrowIfFailedAsync();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var invitationLinkResponse = JsonConvert.DeserializeObject<InvitationLink>(responseContent);
                    return invitationLinkResponse.Id;
                });
        }

        /// <inheritdoc/>
        public Task<WorkspaceResponse> GetStatusAsync(string workspaceId, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_getstatus",
                async (childLogger) =>
                {
                    var response = await HttpClientProvider.HttpClient.GetAsync($"{Path}/{workspaceId}");
                    logger.AddClientHttpResponseDetails(response);

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // If deleted or missing, handle it.
                        return null;
                    }

                    await response.ThrowIfFailedAsync();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var workspaceResponse = JsonConvert.DeserializeObject<WorkspaceResponse>(responseContent);
                    return workspaceResponse;
                });
        }

        private string InvitationLinkPath(string workspaceId) => $"{Path}/{workspaceId}/link";
    }
}
