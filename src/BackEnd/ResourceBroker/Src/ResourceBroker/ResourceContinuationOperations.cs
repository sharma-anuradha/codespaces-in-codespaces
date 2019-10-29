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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

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
        public async Task<ContinuationResult> CreateResource(
            Guid resourceId,
            ResourceType type,
            ResourcePoolResourceDetails details,
            string reason,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resourceId)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolLocation, details.Location.ToString())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolSkuName, details.SkuName)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolResourceType, type.ToString())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolDefinition, details.GetPoolDefinition())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolVersionDefinition, details.GetPoolVersionDefinition())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolImageFamilyName, details.ImageFamilyName)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolImageName, details.ImageName);

            var input = new CreateResourceContinuationInput()
            {
                Type = type,
                ResourcePoolDetails = details,
                ResourceId = resourceId,
                Reason = reason,
            };
            var target = CreateResourceContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, input.ResourceId);
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> StartEnvironment(
            Guid computeResourceId,
            Guid storageResourceId,
            IDictionary<string, string> environmentVariables,
            string reason,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("StorageResourceId", storageResourceId);

            await ValidateAndTraceSystemResource(computeResourceId, reason, logger);

            var input = new StartEnvironmentContinuationInput()
            {
                ResourceId = computeResourceId,
                StorageResourceId = storageResourceId,
                EnvironmentVariables = environmentVariables,
                Reason = reason,
            };
            var target = StartEnvironmentContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, input.ResourceId);
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> DeleteResource(
            Guid resourceId,
            string reason,
            IDiagnosticsLogger logger)
        {
            await ValidateAndTraceSystemResource(resourceId, reason, logger);

            var input = new DeleteResourceContinuationInput()
            {
                ResourceId = resourceId,
                Reason = reason,
            };
            var target = DeleteResourceContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, input.ResourceId);
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> CleanupResource(
            Guid resourceId,
            string environmentId,
            string reason,
            IDiagnosticsLogger logger)
        {
            await ValidateAndTraceSystemResource(resourceId, reason, logger);

            var input = new CleanupResourceContinuationInput()
            {
                ResourceId = resourceId,
                Reason = reason,
                EnvironmentId = environmentId,
            };
            var target = CleanupResourceContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, input.ResourceId);
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> DeleteOrphanedComputeAsync(
            Guid resourceId,
            Guid subscriptionId,
            string resourceGroup,
            string name,
            AzureLocation location,
            IDictionary<string, string> resourceTags,
            string reason,
            IDiagnosticsLogger logger)
        {
            await TraceAzureResource(resourceId, ResourceType.StorageFileShare, resourceTags, reason, logger);

            var input = new VirtualMachineProviderDeleteInput()
            {
                AzureVmLocation = location,
                AzureResourceInfo = new AzureResourceInfo(subscriptionId, resourceGroup, name),
            };
            if (resourceTags != null && resourceTags.ContainsKey(ResourceTagName.ComputeOS))
            {
                if (!Enum.TryParse(resourceTags[ResourceTagName.ComputeOS], true, out ComputeOS computeOS))
                {
                    throw new NotSupportedException($"Resource has a compute OS of {resourceTags[ResourceTagName.ComputeOS]} which is not supported");
                }

                input.ComputeOS = computeOS;
            }

            var target = DeleteOrphanedResourceContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, resourceId);
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> DeleteOrphanedStorageAsync(
            Guid resourceId,
            Guid subscriptionId,
            string resourceGroup,
            string name,
            AzureLocation location,
            IDictionary<string, string> resourceTags,
            string reason,
            IDiagnosticsLogger logger)
        {
            await TraceAzureResource(resourceId, ResourceType.StorageFileShare, resourceTags, reason, logger);

            var input = new FileShareProviderDeleteInput()
            {
                AzureResourceInfo = new AzureResourceInfo(subscriptionId, resourceGroup, name),
            };
            var target = DeleteOrphanedResourceContinuationHandler.DefaultQueueTarget;

            return await Activator.Execute(target, input, logger, resourceId);
        }

        private async Task ValidateAndTraceSystemResource(Guid resourceId, string reason, IDiagnosticsLogger logger)
        {
            var resource = await ResourceRepository.GetAsync(resourceId.ToString(), logger.NewChildLogger());
            if (resource == null)
            {
                throw new ResourceNotFoundException(resourceId);
            }

            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resourceId)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolLocation, resource.Location)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolSkuName, resource.PoolReference.Dimensions.GetValueOrDefault("skuName"))
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolResourceType, resource.Type)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolDefinition, resource.PoolReference.Code)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolVersionDefinition, resource.PoolReference.VersionCode)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolImageFamilyName, resource.PoolReference.Dimensions.GetValueOrDefault("imageFamilyName"))
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolImageName, resource.PoolReference.Dimensions.GetValueOrDefault("imageName"));
        }

        private Task TraceAzureResource(
            Guid resourceId,
            ResourceType type,
            IDictionary<string, string> resourceTags,
            string reason,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resourceId)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolLocation, resourceTags.GetValueOrDefault(ResourceTagName.PoolLocation))
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolSkuName, resourceTags.GetValueOrDefault(ResourceTagName.PoolSkuName))
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolResourceType, type)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolDefinition, resourceTags.GetValueOrDefault(ResourceTagName.PoolDefinition))
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolVersionDefinition, resourceTags.GetValueOrDefault(ResourceTagName.PoolVersionDefinition))
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolImageFamilyName, resourceTags.GetValueOrDefault(ResourceTagName.PoolImageFamilyName))
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolImageName, resourceTags.GetValueOrDefault(ResourceTagName.PoolImageName));

            return Task.CompletedTask;
        }
    }
}
