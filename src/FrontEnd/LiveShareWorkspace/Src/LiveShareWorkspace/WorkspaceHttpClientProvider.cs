// <copyright file="WorkspaceHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace
{
    /// <inheritdoc/>
    public class WorkspaceHttpClientProvider
        : CurrentUserHttpClientProvider<WorkspaceHttpClientProviderOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceHttpClientProvider"/> class.
        /// </summary>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="options">The workspace http client options.</param>
        public WorkspaceHttpClientProvider(
            ICurrentUserProvider currentUserProvider,
            IOptions<WorkspaceHttpClientProviderOptions> options)
            : base(currentUserProvider, options)
        {
        }
    }
}
