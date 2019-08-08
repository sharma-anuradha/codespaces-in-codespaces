// <copyright file="BackEndHttpClientProviderOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient
{
    /// <summary>
    /// Options for the back end HTTP client provider.
    /// </summary>
    public class BackEndHttpClientProviderOptions
        : ICurrentUserHttpClientProviderOptions
    {
        /// <inheritdoc/>
        public string BaseAddress { get; set; }
    }
}
