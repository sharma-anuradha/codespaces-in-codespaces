// <copyright file="ResourceContinuationOperations.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Resource Continuation Operations.
    /// </summary>
    public class ResourceContinuationOperations : IResourceContinuationOperations
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceContinuationOperations"/> class.
        /// </summary>
        /// <param name="activator">Target activator.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        public ResourceContinuationOperations(
            IContinuationTaskActivator activator,
            IResourceRepository resourceRepository)
        {
            Activator = activator;
            ResourceRepository = resourceRepository;
        }

        private IContinuationTaskActivator Activator { get; }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        public async Task<ContinuationResult> CreateAsync(
            Guid resourceId,
            ResourceType type,
            ResourcePoolResourceDetails details,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = BuildLoggingProperties(resourceId, type, details, reason);

            var input = new CreateResourceContinuationInput()
            {
                Type = type,
                ResourcePoolDetails = details,
                ResourceId = resourceId,
                Reason = reason,
                IsAssigned = false,
            };
            var target = CreateResourceContinuationHandlerV2.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, input.ResourceId, loggingProperties);
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> QueueCreateAsync(
            Guid resourceId,
            ResourceType type,
            AllocateExtendedProperties extendedProperties,
            ResourcePoolResourceDetails details,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = BuildLoggingProperties(resourceId, type, details, reason);
            var options = (CreateResourceContinuationInputOptions)default;
            if (extendedProperties != default)
            {
                options = new CreateComputeContinuationInputOptions
                {
                    OSDiskResourceId = extendedProperties.OSDiskResourceID,
                    SubnetResourceId = extendedProperties.SubnetResourceId,
                };
            }

            var input = new CreateResourceContinuationInput()
            {
                Type = type,
                ResourcePoolDetails = details,
                ResourceId = resourceId,
                Reason = reason,
                Options = options,
                IsAssigned = true,
            };
            var target = CreateResourceContinuationHandlerV2.DefaultQueueTarget;

            await Activator.Execute(target, input, logger, input.ResourceId, loggingProperties);
            var resource = await ResourceRepository.GetAsync(resourceId.ToString(), logger.NewChildLogger());

            return resource;
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> StartEnvironmentAsync(
            Guid environmentId,
            Guid computeResourceId,
            Guid? osDiskResourceId,
            Guid? storageResourceId,
            Guid? archiveStorageResourceId,
            IDictionary<string, string> environmentVariables,
            string reason,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("StorageResourceId", storageResourceId)
                .FluentAddBaseValue("OSDiskResourceId", osDiskResourceId)
                .FluentAddBaseValue("OSDiskResourceId", archiveStorageResourceId);

            var loggingProperties = await BuildLoggingProperties(computeResourceId, reason, logger);

            var input = new StartEnvironmentContinuationInput()
            {
                EnvironmentId = environmentId,
                ResourceId = computeResourceId,
                OSDiskResourceId = osDiskResourceId,
                StorageResourceId = storageResourceId,
                ArchiveStorageResourceId = archiveStorageResourceId,
                EnvironmentVariables = environmentVariables,
                Reason = reason,
            };
            var target = StartEnvironmentContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, input.ResourceId, loggingProperties);
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> StartArchiveAsync(
            Guid environmentId,
            Guid blobResourceId,
            Guid fileResourceId,
            string reason,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("BlobResourceId", blobResourceId)
                .FluentAddBaseValue("StorageResourceId", fileResourceId);

            var loggingProperties = await BuildLoggingProperties(blobResourceId, reason, logger);

            var input = new StartArchiveContinuationInput()
            {
                EnvironmentId = environmentId,
                ResourceId = blobResourceId,
                FileShareResourceId = fileResourceId,
                Reason = reason,
            };
            var target = StartArchiveContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, input.ResourceId, loggingProperties);
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> DeleteAsync(
            Guid? environmentId,
            Guid resourceId,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = await BuildLoggingProperties(resourceId, reason, logger);

            var input = new DeleteResourceContinuationInput()
            {
                EnvironmentId = environmentId,
                ResourceId = resourceId,
                Reason = reason,
            };
            var target = DeleteResourceContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, input.ResourceId, loggingProperties);
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> SuspendAsync(
            Guid environmentId,
            Guid resourceId,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = await BuildLoggingProperties(resourceId, reason, logger);

            var input = new CleanupResourceContinuationInput()
            {
                EnvironmentId = environmentId,
                ResourceId = resourceId,
                Reason = reason,
            };
            var target = CleanupResourceContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, input.ResourceId, loggingProperties);
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> DeleteOrphanedResourceAsync(
            Guid resourceId,
            Guid subscriptionId,
            string resourceGroup,
            string name,
            AzureLocation location,
            IDictionary<string, string> resourceTags,
            ResourceType resourceType,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = BuildLoggingProperties(
                resourceId,
                resourceType,
                resourceTags,
                reason);

            var input = new DeleteOrphanedResourceContinuationInput()
            {
                AzureResourceInfo = new AzureResourceInfo(subscriptionId, resourceGroup, name),
                Reason = reason,
                Type = resourceType,
                ResourceTags = resourceTags,
                AzureLocation = location,
            };
            var target = DeleteOrphanedResourceContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, resourceId, loggingProperties);
        }

        private IDictionary<string, string> BuildLoggingProperties(
            Guid resourceId,
            ResourceType type,
            ResourcePoolResourceDetails details,
            string reason)
        {
            return new Dictionary<string, string>()
                {
                    { ResourceLoggingPropertyConstants.ResourceId, resourceId.ToString() },
                    { ResourceLoggingPropertyConstants.OperationReason, reason },
                    { ResourceLoggingPropertyConstants.PoolLocation, details.Location.ToString() },
                    { ResourceLoggingPropertyConstants.PoolSkuName, details.SkuName },
                    { ResourceLoggingPropertyConstants.PoolResourceType, type.ToString() },
                    { ResourceLoggingPropertyConstants.PoolDefinition, details.GetPoolDefinition() },
                    { ResourceLoggingPropertyConstants.PoolVersionDefinition, details.GetPoolVersionDefinition() },
                    { ResourceLoggingPropertyConstants.PoolImageFamilyName, details.ImageFamilyName },
                    { ResourceLoggingPropertyConstants.PoolImageName, details.ImageName },
                };
        }

        private async Task<IDictionary<string, string>> BuildLoggingProperties(
            Guid resourceId,
            string reason,
            IDiagnosticsLogger logger)
        {
            var resource = await ResourceRepository.GetAsync(resourceId.ToString(), logger.NewChildLogger());
            if (resource == null)
            {
                throw new ResourceNotFoundException(resourceId);
            }

            var resourceDetials = resource.GetDetails();

            return new Dictionary<string, string>()
                {
                    { ResourceLoggingPropertyConstants.ResourceId, resourceId.ToString() },
                    { ResourceLoggingPropertyConstants.OperationReason, reason },
                    { ResourceLoggingPropertyConstants.PoolLocation, resource.Location },
                    { ResourceLoggingPropertyConstants.PoolSkuName, resourceDetials.SkuName },
                    { ResourceLoggingPropertyConstants.PoolResourceType, resource.Type.ToString() },
                    { ResourceLoggingPropertyConstants.PoolDefinition, resource.PoolReference?.Code },
                    { ResourceLoggingPropertyConstants.PoolVersionDefinition, resource.PoolReference?.VersionCode },
                    { ResourceLoggingPropertyConstants.PoolImageFamilyName, resourceDetials.ImageFamilyName },
                    { ResourceLoggingPropertyConstants.PoolImageName, resourceDetials.ImageName },
                };
        }

        private IDictionary<string, string> BuildLoggingProperties(
            Guid resourceId,
            ResourceType type,
            IDictionary<string, string> resourceTags,
            string reason)
        {
            return new Dictionary<string, string>()
                {
                    { ResourceLoggingPropertyConstants.ResourceId, resourceId.ToString() },
                    { ResourceLoggingPropertyConstants.OperationReason, reason },
                    { ResourceLoggingPropertyConstants.PoolLocation, resourceTags.GetValueOrDefault(ResourceTagName.PoolLocation) },
                    { ResourceLoggingPropertyConstants.PoolSkuName, resourceTags.GetValueOrDefault(ResourceTagName.PoolSkuName) },
                    { ResourceLoggingPropertyConstants.PoolResourceType, type.ToString() },
                    { ResourceLoggingPropertyConstants.PoolDefinition, resourceTags.GetValueOrDefault(ResourceTagName.PoolDefinition) },
                    { ResourceLoggingPropertyConstants.PoolVersionDefinition, resourceTags.GetValueOrDefault(ResourceTagName.PoolVersionDefinition) },
                    { ResourceLoggingPropertyConstants.PoolImageFamilyName, resourceTags.GetValueOrDefault(ResourceTagName.PoolImageFamilyName) },
                    { ResourceLoggingPropertyConstants.PoolImageName, resourceTags.GetValueOrDefault(ResourceTagName.PoolImageName) },
                };
        }
    }
}
