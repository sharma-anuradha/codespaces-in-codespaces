// <copyright file="ResourceAllocationManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Implements resource allocation manager.
    /// </summary>
    public class ResourceAllocationManager : IResourceAllocationManager
    {
        private static readonly string LogBaseName = nameof(ResourceAllocationManager);

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceAllocationManager"/> class.
        /// </summary>
        /// <param name="resourceBrokerClient">resource broker client.</param>
        public ResourceAllocationManager(IResourceBrokerResourcesExtendedHttpContract resourceBrokerClient)
        {
            ResourceBrokerClient = resourceBrokerClient;
        }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        /// <inheritdoc/>
        public async Task<IEnumerable<ResourceAllocation>> AllocateResourcesAsync(
            Guid environmentId,
            IEnumerable<AllocateRequestBody> allocateRequests,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_allocate_resources",
                async (childLogger) =>
                {
                    var resultResponse = await ResourceBrokerClient.AllocateAsync(
                        environmentId,
                        allocateRequests,
                        logger.NewChildLogger());
                    var result = new List<ResourceAllocation>();
                    foreach (var response in resultResponse)
                    {
                        if (response == null)
                        {
                            throw new InvalidOperationException("Allocate result is invalid.");
                        }

                        result.Add(new ResourceAllocation
                        {
                            ResourceId = response.ResourceId,
                            SkuName = response.SkuName,
                            Location = response.Location,
                            Created = response.Created,
                            Type = response.Type,
                            IsReady = response.IsReady,
                        });
                    }

                    return result;
                },
                swallowException: false);
        }
    }
}
