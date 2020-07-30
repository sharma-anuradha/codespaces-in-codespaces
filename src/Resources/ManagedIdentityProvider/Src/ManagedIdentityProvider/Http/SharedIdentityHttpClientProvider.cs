// <copyright file="SharedIdentityHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Provides a singleton instance of a Shared Identities HTTP Client.
    /// </summary>
    public class SharedIdentityHttpClientProvider : ISharedIdentityHttpClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedIdentityHttpClientProvider"/> class.
        /// </summary>
        /// <param name="resourceAccessor">The Control Plane Resource Accessor.</param>
        public SharedIdentityHttpClientProvider(IControlPlaneAzureResourceAccessor resourceAccessor)
        {
            var handlerChain = new AzureCredentialsDelegatingHandler(
                new HttpClientHandler(),
                resourceAccessor);

            HttpClient = new HttpClient(handlerChain);
        }

        /// <inheritdoc/>
        public HttpClient HttpClient { get; }
    }
}
