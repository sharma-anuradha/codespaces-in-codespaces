// <copyright file="ResourceContinuationOperations.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
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
        /// <param name="jobQueueProducerFactory">Job Queue producer factory instance.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public ResourceContinuationOperations(
            IContinuationTaskActivator activator,
            IResourceRepository resourceRepository,
            IJobQueueProducerFactory jobQueueProducerFactory,
            IConfigurationReader configurationReader)
        {
            Activator = activator;
            ResourceRepository = resourceRepository;
            JobQueueProducerFactory = jobQueueProducerFactory;
            ConfigurationReader = configurationReader;
        }

        private IContinuationTaskActivator Activator { get; }

        private IResourceRepository ResourceRepository { get; }

        private IJobQueueProducerFactory JobQueueProducerFactory { get; }

        private IConfigurationReader ConfigurationReader { get; }

        /// <inheritdoc/>
        public async Task<ContinuationResult> CreateAsync(
            Guid resourceId,
            ResourceType type,
            ResourcePoolResourceDetails details,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
        {
            var consolidatedloggerProperties = BuildLoggingProperties(resourceId, type, details, reason, loggingProperties);

            var options = default(CreateResourceContinuationInputOptions);

            if (type == ResourceType.ComputeVM)
            {
                var separateNetworkAndComputeSubscriptions = await ConfigurationReader.ReadFeatureFlagAsync("separate-network-and-compute-subscriptions", logger.NewChildLogger(), true);

                var createOSDiskRecord = details is ResourcePoolComputeDetails computeDetails && computeDetails.OS == ComputeOS.Windows;

                options = new CreateComputeContinuationInputOptions()
                {
                    SeparateNetworkAndComputeSubscriptions = separateNetworkAndComputeSubscriptions,
                    CreateOSDiskRecord = createOSDiskRecord,
                };
            }

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(CreateResourceContinuationJobHandlerV2.DefaultQueueId).AddJobContinuationAsync(
                    new CreateResourceContinuationJobHandlerV2.Payload()
                    {
                        Type = type,
                        ResourcePoolDetails = details,
                        EntityId = resourceId,
                        Reason = reason,
                        IsAssigned = false,
                        Options = options,
                        LoggerProperties = consolidatedloggerProperties.CreateLoggerProperties(),
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
                var input = new CreateResourceContinuationInput()
                {
                    Type = type,
                    ResourcePoolDetails = details,
                    ResourceId = resourceId,
                    Reason = reason,
                    IsAssigned = false,
                    Options = options,
                };
                var target = CreateResourceContinuationHandlerV2.DefaultQueueTarget;

                return await Activator.Execute(target, input, logger, input.ResourceId, consolidatedloggerProperties);
            }
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> QueueCreateAsync(
            Guid resourceId,
            ResourceType type,
            AllocateExtendedProperties extendedProperties,
            ResourcePoolResourceDetails details,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
        {
            var consolidatedloggerProperties = BuildLoggingProperties(resourceId, type, details, reason, loggingProperties);

            var options = (CreateResourceContinuationInputOptions)default;
            if (extendedProperties != default)
            {
                options = new CreateComputeContinuationInputOptions
                {
                    OSDiskResourceId = extendedProperties.OSDiskResourceID,
                    SubnetResourceId = extendedProperties.SubnetResourceId,
                    HardBoot = extendedProperties.HardBoot,
                    UpdateAgent = extendedProperties.UpdateAgent,
                };
            }

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(CreateResourceContinuationJobHandlerV2.DefaultQueueId).AddJobContinuationAsync(
                    new CreateResourceContinuationJobHandlerV2.Payload()
                    {
                        Type = type,
                        ResourcePoolDetails = details,
                        EntityId = resourceId,
                        Reason = reason,
                        Options = options,
                        IsAssigned = true,
                        LoggerProperties = consolidatedloggerProperties.CreateLoggerProperties(),
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
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

                await Activator.Execute(target, input, logger, input.ResourceId, consolidatedloggerProperties);
            }

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
            IEnumerable<UserSecretData> userSecrets,
            string devcontainerJson,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
        {
            logger.FluentAddBaseValue("StorageResourceId", storageResourceId)
                .FluentAddBaseValue("OSDiskResourceId", osDiskResourceId)
                .FluentAddBaseValue("ArchiveStorageResourceId", archiveStorageResourceId);

            var consolidatedloggerProperties = await BuildLoggingProperties(computeResourceId, reason, logger, loggingProperties);

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(ResumeEnvironmentContinuationJobHandler.DefaultQueueId).AddJobContinuationAsync(
                    new ResumeEnvironmentContinuationJobHandler.Payload()
                    {
                        EnvironmentId = environmentId,
                        EntityId = computeResourceId,
                        OSDiskResourceId = osDiskResourceId,
                        StorageResourceId = storageResourceId,
                        ArchiveStorageResourceId = archiveStorageResourceId,
                        EnvironmentVariables = environmentVariables,
                        UserSecrets = userSecrets,
                        DevContainer = devcontainerJson,
                        Reason = reason,
                        LoggerProperties = consolidatedloggerProperties.CreateLoggerProperties(),
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
                var input = new StartEnvironmentContinuationInput
                {
                    EnvironmentId = environmentId,
                    ResourceId = computeResourceId,
                    OSDiskResourceId = osDiskResourceId,
                    StorageResourceId = storageResourceId,
                    ArchiveStorageResourceId = archiveStorageResourceId,
                    EnvironmentVariables = environmentVariables,
                    UserSecrets = userSecrets,
                    DevContainer = devcontainerJson,
                    Reason = reason,
                };
                var target = ResumeEnvironmentContinuationHandler.DefaultQueueTarget;

                return await Activator.Execute(target, input, logger, input.ResourceId, consolidatedloggerProperties);
            }
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> StartExportAsync(
            Guid environmentId,
            Guid computeResourceId,
            Guid? osDiskResourceId,
            Guid? storageResourceId,
            Guid? archiveStorageResourceId,
            IDictionary<string, string> environmentVariables,
            IEnumerable<UserSecretData> userSecrets,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
        {
            logger.FluentAddBaseValue("StorageResourceId", storageResourceId)
                .FluentAddBaseValue("OSDiskResourceId", osDiskResourceId)
                .FluentAddBaseValue("ArchiveStorageResourceId", archiveStorageResourceId);

            var consolidatedloggerProperties = await BuildLoggingProperties(computeResourceId, reason, logger, loggingProperties);

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(ExportEnvironmentContinuationJobHandler.DefaultQueueId).AddJobContinuationAsync(
                    new ExportEnvironmentContinuationJobHandler.Payload()
                    {
                        EnvironmentId = environmentId,
                        EntityId = computeResourceId,
                        OSDiskResourceId = osDiskResourceId,
                        StorageResourceId = storageResourceId,
                        ArchiveStorageResourceId = archiveStorageResourceId,
                        EnvironmentVariables = environmentVariables,
                        UserSecrets = userSecrets,
                        Reason = reason,
                        LoggerProperties = consolidatedloggerProperties.CreateLoggerProperties(),
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
                var input = new StartExportContinuationInput
                {
                    EnvironmentId = environmentId,
                    ResourceId = computeResourceId,
                    OSDiskResourceId = osDiskResourceId,
                    StorageResourceId = storageResourceId,
                    ArchiveStorageResourceId = archiveStorageResourceId,
                    EnvironmentVariables = environmentVariables,
                    UserSecrets = userSecrets,
                    Reason = reason,
                };
                var target = ExportEnvironmentContinuationHandler.DefaultQueueTarget;

                return await Activator.Execute(target, input, logger, input.ResourceId, consolidatedloggerProperties);
            }
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> StartArchiveAsync(
            Guid environmentId,
            Guid blobResourceId,
            Guid fileResourceId,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
        {
            logger.FluentAddBaseValue("BlobResourceId", blobResourceId)
                .FluentAddBaseValue("StorageResourceId", fileResourceId);

            var consolidatedloggerProperties = await BuildLoggingProperties(blobResourceId, reason, logger, loggingProperties);

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(StartArchiveContinuationJobHandler.DefaultQueueId).AddJobContinuationAsync(
                   new StartArchiveContinuationJobHandler.Payload()
                   {
                       EnvironmentId = environmentId,
                       EntityId = blobResourceId,
                       FileShareResourceId = fileResourceId,
                       Reason = reason,
                       LoggerProperties = consolidatedloggerProperties.CreateLoggerProperties(),
                   },
                   null,
                   logger,
                   CancellationToken.None);
                return null;
            }
            else
            {
                var input = new StartArchiveContinuationInput()
                {
                    EnvironmentId = environmentId,
                    ResourceId = blobResourceId,
                    FileShareResourceId = fileResourceId,
                    Reason = reason,
                };
                var target = StartArchiveContinuationHandler.DefaultQueueTarget;

                return await Activator.Execute(target, input, logger, input.ResourceId, consolidatedloggerProperties);
            }
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> DeleteAsync(
            Guid? environmentId,
            Guid resourceId,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
        {
            var consolidatedloggerProperties = await BuildLoggingProperties(resourceId, reason, logger, loggingProperties);

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(DeleteResourceContinuationJobHandler.DefaultQueueId).AddJobContinuationAsync(
                    new DeleteResourceContinuationJobHandler.Payload()
                    {
                        EnvironmentId = environmentId,
                        EntityId = resourceId,
                        Reason = reason,
                        LoggerProperties = consolidatedloggerProperties.CreateLoggerProperties(),
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
                var input = new DeleteResourceContinuationInput()
                {
                    EnvironmentId = environmentId,
                    ResourceId = resourceId,
                    Reason = reason,
                };
                var target = DeleteResourceContinuationHandler.DefaultQueueTarget;

                return await Activator.Execute(target, input, logger, input.ResourceId, consolidatedloggerProperties);
            }
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> SuspendAsync(
            Guid environmentId,
            Guid resourceId,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
        {
            var consolidatedloggerProperties = await BuildLoggingProperties(resourceId, reason, logger, loggingProperties);

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(CleanupResourceContinuationJobHandler.DefaultQueueId).AddJobContinuationAsync(
                    new CleanupResourceContinuationJobHandler.Payload()
                    {
                        EnvironmentId = environmentId,
                        EntityId = resourceId,
                        Reason = reason,
                        LoggerProperties = consolidatedloggerProperties.CreateLoggerProperties(),
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
                var input = new CleanupResourceContinuationInput()
                {
                    EnvironmentId = environmentId,
                    ResourceId = resourceId,
                    Reason = reason,
                };
                var target = CleanupResourceContinuationHandler.DefaultQueueTarget;

                return await Activator.Execute(target, input, logger, input.ResourceId, consolidatedloggerProperties);
            }
        }

        /// <inheritdoc/>
        public async Task ProcessHeartbeatAsync(HeartBeatInput heartBeatInput, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties)
        {
            const string reason = "HeartbeatReceived";
            var consolidatedloggerProperties = await BuildLoggingProperties(heartBeatInput.ResourceId, reason, logger, loggingProperties);

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(ResourceHeartbeatJobHandler.DefaultQueueId).AddJobAsync(
                    new ResourceHeartbeatJobHandler.Payload()
                    {
                        ResourceId = heartBeatInput.ResourceId,
                        HeartBeatData = heartBeatInput,
                        LoggerProperties = consolidatedloggerProperties.CreateLoggerProperties(),
                    },
                    null,
                    logger,
                    CancellationToken.None);
            }
            else
            {
                var input = new ResourceHeartbeatContinuationInput()
                {
                    ResourceId = heartBeatInput.ResourceId,
                    HeartBeatData = heartBeatInput,
                };

                var target = ResourceHeartbeatContinuationHandler.DefaultQueueTarget;

                await Activator.Execute(target, input, logger, input.ResourceId, consolidatedloggerProperties);
            }
        }

        private IDictionary<string, string> BuildLoggingProperties(
            Guid resourceId,
            ResourceType type,
            ResourcePoolResourceDetails details,
            string reason,
            IDictionary<string, string> loggingProperties)
        {
            var consolidatedloggerProperties = new Dictionary<string, string>()
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

            if (loggingProperties != null)
            {
                foreach (var property in loggingProperties)
                {
                    consolidatedloggerProperties[property.Key] = property.Value;
                }
            }

            return consolidatedloggerProperties;
        }

        private async Task<IDictionary<string, string>> BuildLoggingProperties(
            Guid resourceId,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
        {
            var resource = await ResourceRepository.GetAsync(resourceId.ToString(), logger.NewChildLogger());
            if (resource == null)
            {
                throw new ResourceNotFoundException(resourceId);
            }

            var resourceDetials = resource.GetDetails();

            var consolidatedloggerProperties = new Dictionary<string, string>()
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

            if (loggingProperties != null)
            {
                foreach (var property in loggingProperties)
                {
                    consolidatedloggerProperties[property.Key] = property.Value;
                }
            }

            return consolidatedloggerProperties;
        }

        private Task<bool> IsJobContinuationHandlerEnabledAsync(IDiagnosticsLogger logger)
        {
            return ConfigurationReader.ReadFeatureFlagAsync("job-continuation-handler", logger, true);
        }
    }
}
