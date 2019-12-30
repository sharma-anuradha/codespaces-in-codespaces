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
        public async Task<IEnumerable<ResourceBrokerResource>> CreateResourceSetAsync(
            IEnumerable<CreateResourceRequestBody> input, IDiagnosticsLogger logger)
        {
            var requestUri = ResourceBrokerHttpContract.GetCreateResourceUri();
            var result = await SendAsync<IEnumerable<CreateResourceRequestBody>, IEnumerable<ResourceBrokerResource>>(
                ResourceBrokerHttpContract.PostResourceMethod, requestUri, input, logger.NewChildLogger());
            return result;
        }

        /// <inheritdoc/>
        public async Task<ResourceBrokerResource> GetResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            var requestUri = ResourceBrokerHttpContract.GetGetResourceUri(resourceId);
            var result = await SendAsync<string, ResourceBrokerResource>(ResourceBrokerHttpContract.GetResourceMethod, requestUri, null, logger.NewChildLogger());
            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> TriggerEnvironmentHeartbeatAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            var requestUri = ResourceBrokerHttpContract.GetTriggerEnvironmentHeartbeatUri(resourceId);
            var result = await SendAsync<string, bool>(ResourceBrokerHttpContract.TriggerEnvironmentHeartbeatMethod, requestUri, null, logger.NewChildLogger());
            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            var requestUri = ResourceBrokerHttpContract.GetDeleteResourceUri(resourceId);
            await SendRawAsync<string>(ResourceBrokerHttpContract.DeleteResourceMethod, requestUri, null, logger.NewChildLogger());
            return true;
        }

        /// <inheritdoc/>
        public async Task StartComputeAsync(Guid computeResourceId, StartComputeRequestBody startComputeRequestBody, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(computeResourceId, nameof(computeResourceId));
            var requestUri = ResourceBrokerHttpContract.GetStartComputeUri(computeResourceId);
            _ = await SendAsync<StartComputeRequestBody, string>(
                ResourceBrokerHttpContract.StartComputeMethod, requestUri, startComputeRequestBody, logger.NewChildLogger());
            return;
        }

        /// <inheritdoc/>
        public async Task<bool> CleanupResourceAsync(Guid resourceId, string environmentId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            var requestUri = ResourceBrokerHttpContract.GetCleanupResourceUri(resourceId, environmentId);
            await SendRawAsync<string>(ResourceBrokerHttpContract.PostResourceMethod, requestUri, null, logger.NewChildLogger());
            return true;
        }
    }
}
