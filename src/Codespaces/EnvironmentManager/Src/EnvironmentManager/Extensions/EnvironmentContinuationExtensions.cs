// <copyright file="EnvironmentContinuationExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions
{
    /// <summary>
    /// Environment continuation extensions.
    /// </summary>
    public static class EnvironmentContinuationExtensions
    {
        /// <summary>
        /// Builds queue input based on resource allocation.
        /// </summary>
        /// <param name="resource">Allocated resource.</param>
        /// <returns>Continuation input resource.</returns>
        public static EnvironmentContinuationInputResource BuildQueueInputResource(this ResourceAllocationRecord resource)
        {
            if (resource == default)
            {
                return default;
            }

            return new EnvironmentContinuationInputResource()
            {
                Location = resource.Location,
                SkuName = resource.SkuName,
                Created = resource.Created,
                Type = resource.Type.Value,
                ResourceId = resource.ResourceId,
                IsReady = resource.IsReady,
            };
        }

        /// <summary>
        /// Builds queue input based on resource status response.
        /// </summary>
        /// <param name="resource">Allocated resource.</param>
        /// <returns>Continuation input resource.</returns>
        public static EnvironmentContinuationInputResource BuildQueueInputResource(this StatusResponseBody resource)
        {
            if (resource == default)
            {
                return default;
            }

            return new EnvironmentContinuationInputResource()
            {
                Location = resource.Location,
                SkuName = resource.SkuName,
                Created = resource.Created,
                Type = resource.Type,
                ResourceId = resource.ResourceId,
                IsReady = resource.IsReady,
            };
        }

        /// <summary>
        /// True if it is a failed state.
        /// </summary>
        /// <param name="resourceState">Resource state.</param>
        /// <returns>True if failed state.</returns>
        public static bool IsFailedState(this OperationState? resourceState)
        {
            return resourceState == null
                || resourceState == OperationState.Cancelled
                || resourceState == OperationState.Failed;
        }

        /// <summary>
        /// Builds a resource allocation record.
        /// </summary>
        /// <param name="resource">Resource continuation input.</param>
        /// <returns>Resource allocation.</returns>
        public static ResourceAllocationRecord BuildResourceRecord(this EnvironmentContinuationInputResource resource)
        {
            if (resource == default)
            {
                return default;
            }

            return new ResourceAllocationRecord()
            {
                Location = resource.Location,
                SkuName = resource.SkuName,
                Created = resource.Created,
                Type = resource.Type,
                ResourceId = resource.ResourceId,
                IsReady = resource.IsReady,
            };
        }
    }
}
