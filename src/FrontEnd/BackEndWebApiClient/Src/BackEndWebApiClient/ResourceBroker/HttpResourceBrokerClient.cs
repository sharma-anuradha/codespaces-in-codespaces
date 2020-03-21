// <copyright file="HttpResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker
{
    /// <summary>
    /// An http resource broker client.
    /// </summary>
    public class HttpResourceBrokerClient : HttpClientBase, IResourceBrokerResourcesExtendedHttpContract
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
        public async Task<AllocateResponseBody> AllocateAsync(Guid environmentId, AllocateRequestBody resource, IDiagnosticsLogger logger)
        {
            return (await AllocateAsync(environmentId, new List<AllocateRequestBody> { resource }, logger)).Single();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AllocateResponseBody>> AllocateAsync(Guid environmentId, IEnumerable<AllocateRequestBody> resources, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            Requires.NotNullOrEmpty(resources, nameof(resources));

            var requestUri = ResourceBrokerHttpContract.GetAllocateResourceUri(environmentId);
            var result = await SendAsync<IEnumerable<AllocateRequestBody>, IEnumerable<AllocateResponseBody>>(
                ResourceBrokerHttpContract.AllocateResourceMethod, requestUri, resources, logger.NewChildLogger());
            if (result == null || result.Count() != resources.Count())
            {
                throw new ArgumentException("Invalid response where result count did not match input count.");
            }

            return result;
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(
            Guid environmentId,
            StartRequestAction action,
            StartRequestBody resource,
            IDiagnosticsLogger logger)
        {
            return StartAsync(environmentId, action, new List<StartRequestBody> { resource }, logger);
        }

        /// <inheritdoc/>
        public async Task<bool> StartAsync(
            Guid environmentId,
            StartRequestAction action,
            IEnumerable<StartRequestBody> resources,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            Requires.NotNullOrEmpty(resources, nameof(resources));
            var requestUri = ResourceBrokerHttpContract.GetStartResourceUri(environmentId, action);
            return await SendAsync<IEnumerable<StartRequestBody>, bool>(
                ResourceBrokerHttpContract.StartResourceMethod, requestUri, resources, logger.NewChildLogger());
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            return SuspendAsync(environmentId, new List<Guid> { resourceId }, logger);
        }

        /// <inheritdoc/>
        public async Task<bool> SuspendAsync(Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            Requires.NotNullOrEmpty(resources, nameof(resources));
            var requestUri = ResourceBrokerHttpContract.GetSuspendResourceUri(environmentId);
            return await SendAsync<IEnumerable<Guid>, bool>(
                ResourceBrokerHttpContract.SuspendResourceMethod, requestUri, resources, logger.NewChildLogger());
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            return DeleteAsync(environmentId, new List<Guid> { resourceId }, logger);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            Requires.NotNullOrEmpty(resources, nameof(resources));
            var requestUri = ResourceBrokerHttpContract.GetDeleteResourceUri(environmentId);
            return await SendAsync<IEnumerable<Guid>, bool>(
                ResourceBrokerHttpContract.DeleteResourceMethod, requestUri, resources, logger.NewChildLogger());
        }

        /// <inheritdoc/>
        public async Task<StatusResponseBody> StatusAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            return (await StatusAsync(environmentId, new List<Guid> { resourceId }, logger)).Single();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<StatusResponseBody>> StatusAsync(Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            Requires.NotNullOrEmpty(resources, nameof(resources));
            var requestUri = ResourceBrokerHttpContract.GetStatusResourceUri(environmentId, resources);
            var result = await SendAsync<IEnumerable<Guid>, IEnumerable<StatusResponseBody>>(
                ResourceBrokerHttpContract.StatusResourceMethod, requestUri, resources, logger.NewChildLogger());
            if (result == null || result.Count() != resources.Count())
            {
                throw new ArgumentException("Invalid response where result count did not match input count.");
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessHeartbeatAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            var requestUri = ResourceBrokerHttpContract.GetProcessHeartbeatUri(environmentId, resourceId);
            var result = await SendAsync<string, bool>(
                ResourceBrokerHttpContract.ProcessHeartbeatMethod, requestUri, null, logger.NewChildLogger());
            return result;
        }
    }
}
