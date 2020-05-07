// <copyright file="ResourceTagExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using ResourceType = Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.ResourceType;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    /// Resource tag extensions.
    /// </summary>
    public static class ResourceTagExtensions
    {
        private const string UnknownString = "unknown";

        /// <summary>
        /// Creates resource tags from record and reason.
        /// </summary>
        /// <param name="resource">Resource record.</param>
        /// <param name="reason">Reason string.</param>
        /// <returns>Resource tags.</returns>
        public static IDictionary<string, string> GetResourceTags(this ResourceRecord resource, string reason)
        {
            return new Dictionary<string, string>()
            {
                { ResourceTagName.ResourceId, resource.Id ?? UnknownString },
                { ResourceTagName.ResourceName, resource?.AzureResourceInfo?.Name ?? UnknownString },
                { ResourceTagName.ResourceType, resource.Type.ToString() },
                { ResourceTagName.PoolLocation, resource.Location ?? UnknownString },
                { ResourceTagName.PoolSkuName, resource.PoolReference.Dimensions.GetValueOrDefault("skuName", UnknownString) },
                { ResourceTagName.PoolDefinition, resource.PoolReference.Code ?? UnknownString },
                { ResourceTagName.PoolVersionDefinition, resource.PoolReference.VersionCode ?? UnknownString },
                { ResourceTagName.PoolImageFamilyName, resource.PoolReference.Dimensions.GetValueOrDefault("imageFamilyName", UnknownString) },
                { ResourceTagName.PoolImageName, resource.PoolReference.Dimensions.GetValueOrDefault("imageName", UnknownString) },
                { ResourceTagName.OperationReason, reason ?? UnknownString },
                { ResourceTagName.ResourceComponentRecordIds, resource.GetResourceComponentIds() },
            };
        }

        /// <summary>
        /// Checks if a component is backed by a backend resource record.
        /// </summary>
        /// <param name="resourceTags">Resource tags.</param>
        /// <param name="resourceRepository">Resource repository.</param>
        /// <param name="resourceType">Resource type.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>True if the component is backed by a record.</returns>
        public static async Task<ResourceComponent> GetBackingComponentRecordAsync(
            this IDictionary<string, string> resourceTags,
            IResourceRepository resourceRepository,
            ResourceType resourceType,
            IDiagnosticsLogger logger)
        {
            if (resourceTags != default && resourceTags.ContainsKey(ResourceTagName.ResourceComponentRecordIds))
            {
                var componentRecordIds = resourceTags[ResourceTagName.ResourceComponentRecordIds];
                if (!string.IsNullOrWhiteSpace(componentRecordIds))
                {
                    var recordsIds = componentRecordIds.Split(',').Select(x => x.Trim());
                    foreach (var recordId in recordsIds)
                    {
                        try
                        {
                            var resourceRecord = await resourceRepository.GetAsync(recordId, logger.NewChildLogger());
                            if (resourceRecord != null && resourceRecord.Type == resourceType && string.IsNullOrEmpty(resourceRecord.AzureResourceInfo?.Name))
                            {
                                // Return true, if there is a backing record of the same type and has a valid AzureResourceInfo.
                                return new ResourceComponent(resourceRecord.Type, resourceRecord.AzureResourceInfo, recordId, true);
                            }
                        }
                        catch
                        {
                            // Ignore any errors here.
                        }
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Gets the compute OS from the resource tags.
        /// </summary>
        /// <param name="resourceTags">Resource tags.</param>
        /// <returns>Compute OS.</returns>
        public static ComputeOS GetComputeOS(this IDictionary<string, string> resourceTags)
        {
            if (resourceTags != default && resourceTags.ContainsKey(ResourceTagName.ComputeOS))
            {
                if (!Enum.TryParse(resourceTags[ResourceTagName.ComputeOS], true, out ComputeOS computeOS))
                {
                    throw new NotSupportedException($"Resource has a compute OS of {resourceTags[ResourceTagName.ComputeOS]} which is not supported");
                }

                return computeOS;
            }

            throw new NotSupportedException($"Resource has no compute OS type.");
        }

        private static string GetResourceComponentIds(this ResourceRecord resource)
        {
            if (resource == default || resource.AzureResourceInfo == default || resource.Components?.Items == default)
            {
                return UnknownString;
            }

            var result = string.Join(",", resource.Components?.Items?.Values.Where(x => !string.IsNullOrWhiteSpace(x.ResourceRecordId)).Select(x => x.ResourceRecordId));
            if (string.IsNullOrWhiteSpace(result))
            {
                return UnknownString;
            }

            return result;
        }
    }
}
