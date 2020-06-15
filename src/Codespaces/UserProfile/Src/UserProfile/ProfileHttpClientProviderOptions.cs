// <copyright file="ProfileHttpClientProviderOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// Options for the User Profile provider.
    /// </summary>
    public class ProfileHttpClientProviderOptions
        : IHttpClientProviderOptions
    {
        /// <inheritdoc/>
        public string BaseAddress { get; set; }
    }
}
