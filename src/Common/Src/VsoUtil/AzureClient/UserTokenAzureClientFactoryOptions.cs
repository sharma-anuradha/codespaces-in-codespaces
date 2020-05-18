// <copyright file="UserTokenAzureClientFactoryOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.AzureClient
{
    /// <summary>
    /// Options for <see cref="UserTokenAzureClientFactory"/>.
    /// </summary>
    public class UserTokenAzureClientFactoryOptions
    {
        /// <summary>
        /// Gets or sets the user access token.
        /// </summary>
        public string AccessToken { get; set; }
    }
}
