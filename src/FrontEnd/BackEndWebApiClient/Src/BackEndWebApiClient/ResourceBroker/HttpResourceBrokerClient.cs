// <copyright file="HttpResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker
{
    /// <summary>
    /// An http resource broker client.
    /// </summary>
    public class HttpResourceBrokerClient : HttpClientBase, IResourceBrokerResourcesHttpContract
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResourceBrokerClient"/> class.
        /// </summary>
        /// <param name="httpClientProvider">The backend http client provider.</param>
        public HttpResourceBrokerClient(
            ICurrentUserHttpClientProvider<BackEndHttpClientProviderOptions> httpClientProvider)
            : base(httpClientProvider)
        {
        }

        /// <inheritdoc/>
        public async Task<ResourceBrokerResource> GetAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            var requestUri = ResourceBrokerHttpContract.GetGetResourceUri(resourceId);
            var result = await SendAsync<string, ResourceBrokerResource>(
                ResourceBrokerHttpContract.GetResourceMethod, requestUri, null, logger.NewChildLogger());
            return result;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AllocateResponseBody>> AllocateAsync(
            Guid environmentId, IEnumerable<AllocateRequestBody> input, IDiagnosticsLogger logger)
        {
            var requestUri = ResourceBrokerHttpContract.GetAllocateResourceUri(environmentId);
            var result = await SendAsync<IEnumerable<AllocateRequestBody>, IEnumerable<AllocateResponseBody>>(
                ResourceBrokerHttpContract.PostResourceMethod, requestUri, input, logger.NewChildLogger());
            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> StartAsync(
            Guid computeResourceId, StartResourceRequestBody startResourceSetRequestBody, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(computeResourceId, nameof(computeResourceId));
            var requestUri = ResourceBrokerHttpContract.GetStartResourceUri(computeResourceId);
            _ = await SendAsync<StartResourceRequestBody, string>(
                ResourceBrokerHttpContract.StartComputeMethod, requestUri, startResourceSetRequestBody, logger.NewChildLogger());
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> SuspendAsync(Guid environmentId, IEnumerable<SuspendRequestBody> suspendRequestBody, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            var requestUri = ResourceBrokerHttpContract.GetSuspendResourceUri(environmentId);
            var response = await SendAsync<IEnumerable<SuspendRequestBody>, bool?>(
                ResourceBrokerHttpContract.PostResourceMethod, requestUri, suspendRequestBody, logger.NewChildLogger());
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            var requestUri = ResourceBrokerHttpContract.GetDeleteResourceUri(resourceId);
            await SendRawAsync<string>(
                ResourceBrokerHttpContract.DeleteResourceMethod, requestUri, null, logger.NewChildLogger());
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessHeartbeatAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            var requestUri = ResourceBrokerHttpContract.GetProcessHeartbeatUri(resourceId);
            var result = await SendAsync<string, bool>(
                ResourceBrokerHttpContract.TriggerEnvironmentHeartbeatMethod, requestUri, null, logger.NewChildLogger());
            return result;
        }
    }
}
