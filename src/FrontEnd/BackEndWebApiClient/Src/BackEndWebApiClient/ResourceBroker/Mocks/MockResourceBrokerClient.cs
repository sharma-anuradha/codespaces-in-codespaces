// <copyright file="MockResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        private static readonly string MockResourceGroup = "mock-resource-group";

        /// <inheritdoc/>
        public async Task<ResourceBrokerResource> CreateResourceAsync(CreateResourceRequestBody allocateRequestBody, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            var mockInstanceId = Guid.NewGuid();
            var result = new ResourceBrokerResource
            {
                ResourceIdToken = $"vssaas/resourcetypes/{allocateRequestBody.Type.ToString()}/instances/{mockInstanceId}/subscriptions/{MockSubscriptionId}/resourcegroups/{MockResourceGroup}/locations/{allocateRequestBody.Location.ToString().ToLowerInvariant()}",
                Created = DateTime.UtcNow,
                Location = allocateRequestBody.Location,
                SkuName = allocateRequestBody.SkuName,
                Type = allocateRequestBody.Type,
            };
            return result;
        }

        /// <inheritdoc/>
        public Task<ResourceBrokerResource> GetResourceAsync(string resourceIdToken, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>

        /// <inheritdoc/>
        public Task<bool> DeleteResourceAsync(string resourceIdToken, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<StartComputeResponseBody> StartComputeAsync(string computeResourceTokenId, StartComputeRequestBody startComputeRequestBody, IDiagnosticsLogger logger)
        {
            return Task.FromResult(new StartComputeResponseBody { });
        }
    }
}
