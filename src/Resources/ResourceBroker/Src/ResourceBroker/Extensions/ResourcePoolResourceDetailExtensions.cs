// <copyright file="ResourcePoolResourceDetailExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    /// Resource Pool Resource Detail Extensions.
    /// </summary>
    public static class ResourcePoolResourceDetailExtensions
    {
        /// <summary>
        /// Name of the osdisk resource type.
        /// Note: Don't change the name.
        /// </summary>
        public const string OSDisk = nameof(OSDisk);

        /// <summary>
        /// Creates ResourceRecord object with the provided pool details.
        /// </summary>
        /// <param name="computeDetails">Compute resource pool details.</param>
        /// <returns>OSDisk resource record.</returns>
        public static ResourceRecord CreateOSDiskRecord(this ResourcePoolComputeDetails computeDetails)
        {
            var id = Guid.NewGuid();
            var time = DateTime.UtcNow;
            var type = ResourceType.OSDisk;
            var location = computeDetails.Location;
            var skuName = OSDisk;

            // Core record
            var resource = ResourceRecord.Build(id, time, type, location, skuName);
            resource.IsAssigned = false;
            resource.IsReady = false;
            resource.ProvisioningStatus = OperationState.InProgress;

            var osDiskPoolDetails = new ResourcePoolOSDiskDetails()
            {
                ImageFamilyName = computeDetails.ImageFamilyName,
                ImageName = computeDetails.ImageName,
                Location = computeDetails.Location,
                OS = computeDetails.OS,
                SkuFamily = computeDetails.SkuFamily,
                SkuName = computeDetails.SkuName,
                VmAgentImageName = computeDetails.VmAgentImageName,
                VmAgentImageFamilyName = computeDetails.VmAgentImageFamilyName,
            };

            // Copy over pool reference detail.
            resource.PoolReference = new ResourcePoolDefinitionRecord
            {
                Code = osDiskPoolDetails.GetPoolDefinition(),
                VersionCode = osDiskPoolDetails.GetPoolVersionDefinition(),
                Dimensions = osDiskPoolDetails.GetPoolDimensions(),
            };

            return resource;
        }

        /// <summary>
        /// Get code for pool queue resource that respresents resource type PoolQueue.
        /// </summary>
        /// <param name="poolCode">pool code.</param>
        /// <returns>pool queue code.</returns>
        public static string GetPoolQueueDefinition(this string poolCode)
        {
            return $"{poolCode}-PoolQueue";
        }

        /// <summary>
        /// Get poolCode for resource pool, that uses this poolQueue for queue resource allocation.
        /// </summary>
        /// <param name="poolQueueCode">pool code.</param>
        /// <returns>pool queue code.</returns>
        public static string GetPoolCodeForQueue(this string poolQueueCode)
        {
            var codeComponent = poolQueueCode.Split("-");
            if (codeComponent.Length != 2)
            {
                return default;
            }

            return codeComponent[0];
        }
    }
}
