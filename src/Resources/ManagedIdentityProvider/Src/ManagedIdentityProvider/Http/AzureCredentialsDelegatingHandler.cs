// <copyright file="AzureCredentialsDelegatingHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// A delegating handler to append Azure Credentials.
    /// </summary>
    public class AzureCredentialsDelegatingHandler : DelegatingHandler
    {
        private readonly IControlPlaneAzureResourceAccessor credentialsProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCredentialsDelegatingHandler"/> class.
        /// </summary>
        /// <param name="innerHandler">The inner handler.</param>
        /// <param name="resourceAccessor">The resource accessor.</param>
        public AzureCredentialsDelegatingHandler(
            HttpMessageHandler innerHandler,
            IControlPlaneAzureResourceAccessor resourceAccessor)
            : base(innerHandler)
        {
            this.credentialsProvider = resourceAccessor;
        }

        /// <inheritdoc/>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var azureCredentials = await credentialsProvider.GetAzureCredentialsAsync();

            // apply the Azure Credentials to the request
            await azureCredentials.ProcessHttpRequestAsync(request, cancellationToken);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
