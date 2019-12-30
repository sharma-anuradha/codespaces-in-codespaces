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
    public class MockResourceBrokerClient : IResourceBrokerResourcesHttpContract
    {
        private static readonly Guid MockSubscriptionId = Guid.NewGuid();

        /// <inheritdoc/>
        public async Task<IEnumerable<ResourceBrokerResource>> CreateResourceSetAsync(
            IEnumerable<CreateResourceRequestBody> createResourcesRequestBody, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;

            var now = DateTime.UtcNow;
            var results = new List<ResourceBrokerResource>();
            foreach (var createResourceRequestBody in createResourcesRequestBody)
            {
                var mockInstanceId = Guid.NewGuid();
                results.Add(new ResourceBrokerResource
                {
                    ResourceId = mockInstanceId,
                    Created = DateTime.UtcNow,
                    Location = createResourceRequestBody.Location,
                    SkuName = createResourceRequestBody.SkuName,
                    Type = createResourceRequestBody.Type,
                });
            }

            return results;
        }

        /// <inheritdoc/>
        public Task<ResourceBrokerResource> GetResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> DeleteResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task StartComputeAsync(Guid computeResource, StartComputeRequestBody startComputeRequestBody, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> TriggerEnvironmentHeartbeatAsync(Guid id, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> CleanupResourceAsync(Guid resourceId, string environmentId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }
    }
}
