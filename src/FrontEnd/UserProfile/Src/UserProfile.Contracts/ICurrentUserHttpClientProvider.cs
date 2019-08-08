// <copyright file="ICurrentUserHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// A provider for the an HTTP client that uses the current user's bearer token.
    /// </summary>
    /// <typeparam name="TOptions">The concrete options type that identifies this HttpClient singleton.</typeparam>
    public interface ICurrentUserHttpClientProvider<out TOptions> : IHttpClientProvider
        where TOptions : class, ICurrentUserHttpClientProviderOptions, new()
    {
    }
}
