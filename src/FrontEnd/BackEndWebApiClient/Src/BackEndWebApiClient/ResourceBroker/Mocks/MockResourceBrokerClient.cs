// <copyright file="MockResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks
{
    /// <summary>
    /// A mock in-memory resource broker client. No-ops all calls.
    /// </summary>
    public class MockResourceBrokerClient : IResourceBrokerClient
    {
        private static readonly Guid MockSubscriptionId = Guid.NewGuid();
        private static readonly string MockResourceGroup = "mock-resource-group";

        /// <inheritdoc/>
        public async Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            var mockInstanceId = Guid.NewGuid();
            var result = new AllocateResult
            {
                ResourceIdToken = $"vssaas/resourcetypes/{input.Type.ToString()}/instances/{mockInstanceId}/subscriptions/{MockSubscriptionId}/resourcegroups/{MockResourceGroup}/locations/{input.Location.ToString().ToLowerInvariant()}",
                Created = DateTime.UtcNow,
                Location = input.Location,
                SkuName = input.SkuName,
                Type = input.Type,
            };
            return result;
        }

        /// <inheritdoc/>
        public Task<bool> DeallocateAsync(string resourceIdToken)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<BindResult> BindComputeToStorage(BindInput input)
        {
            return Task.FromResult(new BindResult { });
        }
    }
}
