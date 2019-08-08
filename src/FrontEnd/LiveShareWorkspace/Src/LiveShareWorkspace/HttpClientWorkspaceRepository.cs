// <copyright file="HttpClientWorkspaceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace
{
    /// <inheritdoc/>
    public class HttpClientWorkspaceRepository : IWorkspaceRepository
    {
        private const string Path = "workspace";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientWorkspaceRepository"/> class.
        /// </summary>
        /// <param name="httpClientProvider">The http client provider.</param>
        public HttpClientWorkspaceRepository(
            ICurrentUserHttpClientProvider<WorkspaceHttpClientProviderOptions> httpClientProvider)
        {
            HttpClientProvider = httpClientProvider;
        }

        private IHttpClientProvider HttpClientProvider { get; }

        /// <inheritdoc/>
        public async Task<WorkspaceResponse> CreateAsync(WorkspaceRequest workspace)
        {
            var response = await HttpClientProvider.HttpClient.PostAsync(Path, workspace, new JsonMediaTypeFormatter());
            await response.ThrowIfFailedAsync();
            var workspaceResponse = await response.Content.ReadAsAsync<WorkspaceResponse>();
            return workspaceResponse;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string workspaceId)
        {
            var response = await HttpClientProvider.HttpClient.DeleteAsync($"{Path}/{workspaceId}");
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                // The call may fail, as the workspace may already been cleaned up. Ignore failure response.
                await response.ThrowIfFailedAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<WorkspaceResponse> GetStatusAsync(string workspaceId)
        {
            var response = await HttpClientProvider.HttpClient.GetAsync($"{Path}/{workspaceId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // If deleted or missing, handle it.
                return null;
            }

            await response.ThrowIfFailedAsync();
            var workspaceResponse = await response.Content.ReadAsAsync<WorkspaceResponse>();
            return workspaceResponse;
        }
    }
}
