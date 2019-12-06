// <copyright file="HttpClientWorkspaceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace
{
    /// <inheritdoc/>
    public class HttpClientWorkspaceRepository : IWorkspaceRepository
    {
        private const string LogBaseName = "httpclientworkspacerepository";
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
        public Task<WorkspaceResponse> CreateAsync(WorkspaceRequest workspace, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_create",
                async (childLogger) =>
                {
                    var response = await HttpClientProvider.HttpClient.PostAsync(Path, workspace, new JsonMediaTypeFormatter());
                    logger.AddClientHttpResponseDetails(response);

                    await response.ThrowIfFailedAsync();

                    var workspaceResponse = await response.Content.ReadAsAsync<WorkspaceResponse>();
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

                    var workspaceResponse = await response.Content.ReadAsAsync<WorkspaceResponse>();
                    return workspaceResponse;
                });
        }
    }
}
