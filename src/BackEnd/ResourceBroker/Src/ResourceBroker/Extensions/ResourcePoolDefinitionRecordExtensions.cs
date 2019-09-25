// <copyright file="ResourcePoolDefinitionRecordExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    /// Extension methods for ResourcePoolDefinitionRecord.
    /// </summary>
    public static class ResourcePoolDefinitionRecordExtensions
    {
        /// <summary>
        /// Gets the ComputeOS for a ResourcePoolDefinitionRecord.
        /// </summary>
        /// <param name="record">The record to query.</param>
        /// <returns>Returns a ComputeOS.</returns>
        public static ComputeOS GetComputeOS(this ResourcePoolDefinitionRecord record)
        {
            // Assume null Dimension is from a record created before Dimension was added, so it must be a Linux VM
            if (record.Dimensions == null || !record.Dimensions.TryGetValue(ResourcePoolDimensionsKeys.ComputeOS, out var computeOS))
            {
                return ComputeOS.Linux;
            }

            return computeOS.ToEnum<ComputeOS>();
        }
    }
}
