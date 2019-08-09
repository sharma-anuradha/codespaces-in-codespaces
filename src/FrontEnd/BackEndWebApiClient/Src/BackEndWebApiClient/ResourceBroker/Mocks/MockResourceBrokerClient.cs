// <copyright file="MockResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks
{
    /// <summary>
    /// A mock in-memory resource broker client. No-ops all calls.
    /// </summary>
    public class MockResourceBrokerClient : IResourceBrokerHttpContract
    {
        private static readonly Guid MockSubscriptionId = Guid.NewGuid();
        private static readonly string MockResourceGroup = "mock-resource-group";

        /// <inheritdoc/>
        public async Task<AllocateResponseBody> AllocateAsync(AllocateRequestBody allocateRequestBody, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            var mockInstanceId = Guid.NewGuid();
            var result = new AllocateResponseBody
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
        public Task<bool> DeallocateAsync(string resourceIdToken, IDiagnosticsLogger logger)
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
