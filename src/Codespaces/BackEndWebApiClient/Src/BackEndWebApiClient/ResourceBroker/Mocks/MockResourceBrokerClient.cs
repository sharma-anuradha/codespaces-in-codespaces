// <copyright file="MockResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks
{
    /// <summary>
    /// A mock in-memory resource broker client. No-ops all calls.
    /// </summary>
    public class MockResourceBrokerClient : IResourceBrokerResourcesExtendedHttpContract
    {
        /// <inheritdoc/>
        public Task<ResourceBrokerResource> GetAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<AllocateResponseBody> AllocateAsync(Guid environmentId, AllocateRequestBody resource, IDiagnosticsLogger logger)
        {
            var result = new AllocateResponseBody
            {
                ResourceId = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                Location = resource.Location,
                SkuName = resource.SkuName,
                Type = resource.Type,
            };

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AllocateResponseBody>> AllocateAsync(Guid environmentId, IEnumerable<AllocateRequestBody> resources, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            var results = new List<AllocateResponseBody>();
            foreach (var resource in resources)
            {
                results.Add(await AllocateAsync(environmentId, resource, logger));
            }

            return results;
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(Guid environmentId, StartRequestAction action, StartRequestBody resource, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(Guid environmentId, StartRequestAction action, IEnumerable<StartRequestBody> resources, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<StatusResponseBody> StatusAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            var result = new StatusResponseBody { ResourceId = resourceId };

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<StatusResponseBody>> StatusAsync(Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger)
        {
            var results = new List<StatusResponseBody>();
            foreach (var resource in resources)
            {
                results.Add(await StatusAsync(environmentId, resource, logger));
            }

            return results;
        }

        /// <inheritdoc/>
        public Task<bool> ProcessHeartbeatAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }
    }
}
