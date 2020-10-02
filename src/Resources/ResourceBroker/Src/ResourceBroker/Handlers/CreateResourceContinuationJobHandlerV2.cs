// <copyright file="CreateResourceContinuationJobHandlerV2.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages creating of environment.
    /// </summary>
    public class CreateResourceContinuationJobHandlerV2
        : ResourceContinuationJobHandlerBase<CreateResourceContinuationJobHandlerV2.Payload, EmptyContinuationState, CreateResourceContinuationJobHandlerV2.CreateResourceContinuationResult>
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-create-resource";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateResourceContinuationHandlerV2"/> class.
        /// </summary>
        /// <param name="diskProvider">The disk provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="creationStrategies">Disk provider.</param>
        /// <param name="resourceStateManager">Request state Manager to update resource state.</param>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        public CreateResourceContinuationJobHandlerV2(
            IDiskProvider diskProvider,
            IResourceRepository resourceRepository,
            IEnumerable<ICreateResourceStrategy> creationStrategies,
            IResourceStateManager resourceStateManager,
            IJobQueueProducerFactory jobQueueProducerFactory)
            : base(resourceRepository, resourceStateManager, jobQueueProducerFactory)
        {
            CreationStrategies = creationStrategies;
            DiskProvider = diskProvider;
        }

        /// <inheritdoc/>
        public override string QueueId => DefaultQueueId;

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerCreate;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Provisioning;

        /// <summary>
        /// Gets resource creation strategies.
        /// </summary>
        protected IEnumerable<ICreateResourceStrategy> CreationStrategies { get; }

        private IDiskProvider DiskProvider { get; }

        /// <inheritdoc/>
        protected override async Task<IEntityRecordRef<ResourceRecord>> FetchReferenceAsync(Payload payload, IDiagnosticsLogger logger)
        {
            // If we have a reference use that
            if (!payload.IsCreated)
            {
                var resourceRecordRef = await CreateReferenceAsync(payload, logger);
                payload.IsCreated = true;
                return resourceRecordRef;
            }

            return await base.FetchReferenceAsync(payload, logger);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationJobResult<EmptyContinuationState, CreateResourceContinuationResult>> ContinueAsync(Payload payload, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Note: next initial steps will create the continuation input adapter
            // that will be used in most of the calls used by the create strategy entity
            var resource = (ResourceRecordRef)record;

            var input = new CreateResourceContinuationInput()
            {
                ResourcePoolDetails = payload.ResourcePoolDetails,
                Type = payload.Type,
                IsAssigned = payload.IsAssigned,
                Options = payload.Options,
                Preserve = payload.Preserve,
                ResourceId = payload.EntityId,
                Reason = payload.Reason,
            };
            var createStrategy = CreationStrategies.Where(s => s.CanHandle(input)).Single();

            // If the input is empty we construct one that will be deserialized when iun progress
            if (payload.CreateInput == null)
            {
                payload.CreateInput = await createStrategy.BuildCreateOperationInputAsync(input, resource, logger);
            }

            input.OperationInput = payload.CreateInput;

            var result = await createStrategy.RunCreateOperationCoreAsync(input, resource, logger);

            // Keep track of the returned next input
            if (result.NextInput != null)
            {
                payload.CreateInput = result.NextInput;
            }

            // Update the components provisioning status.
            if (result.Status == OperationState.Succeeded && result.Components?.Items != default)
            {
                foreach (var component in result.Components.Items.Values)
                {
                    if (component.ResourceRecordId != default)
                    {
                        var componentResourceReference = await FetchReferenceAsync(Guid.Parse(component.ResourceRecordId), logger.NewChildLogger());

                        if (component.AzureResourceInfo == default)
                        {
                            component.AzureResourceInfo = await AcquireComponentAzureResourceAsync(resource, result, component, componentResourceReference, logger);

                            if (componentResourceReference.Value.ProvisioningStatus != result.Status ||
                                !component.AzureResourceInfo.Equals(componentResourceReference.Value.AzureResourceInfo))
                            {
                                await UpdateRecordAsync(
                                    payload,
                                    componentResourceReference,
                                    (componentRecord, childLogger) =>
                                    {
                                        componentRecord.ProvisioningStatus = result.Status;
                                        componentRecord.ProvisioningStatusChanged = DateTime.UtcNow;
                                        componentRecord.AzureResourceInfo = component.AzureResourceInfo;

                                        return Task.FromResult(true);
                                    },
                                    logger.NewChildLogger());
                            }
                        }
                    }
                }
            }

            // Make sure we bring over the Resource info if we have it
            if (ResourceInfoChanged(resource, result))
            {
                // Retry till we succeed
                await logger.RetryOperationScopeAsync(
                    $"{LogBaseName}_record_update",
                    async (IDiagnosticsLogger innerLogger) =>
                    {
                        resource.Value = (await FetchReferenceAsync(payload, innerLogger)).Value;

                        resource.Value.AzureResourceInfo = result.AzureResourceInfo;

                        resource.Value.Components = result.Components;

                        resource.Value = await ResourceRepository.UpdateAsync(resource.Value, innerLogger.NewChildLogger());
                    });
            }

            return ToContinuationInfo(result, payload);
        }

        private static bool ResourceInfoChanged(ResourceRecordRef resource, ResourceCreateContinuationResult result)
        {
            return (resource.Value.AzureResourceInfo != default && !resource.Value.AzureResourceInfo.Equals(result.AzureResourceInfo))
                    || (resource.Value.AzureResourceInfo == default && result.AzureResourceInfo != default)
                    || (resource.Value.Components != default && !resource.Value.Components.Equals(result.Components))
                    || (resource.Value.Components == default && result.Components != default);
        }

        private async Task<ResourceRecordRef> CreateReferenceAsync(Payload payload, IDiagnosticsLogger logger)
        {
            // Common properties
            var id = payload.EntityId;
            var time = DateTime.UtcNow;
            var type = payload.Type;
            var location = payload.ResourcePoolDetails.Location;
            var skuName = payload.ResourcePoolDetails.SkuName;
            var poolReference = new ResourcePoolDefinitionRecord
            {
                Code = payload.ResourcePoolDetails.GetPoolDefinition(),
                VersionCode = payload.ResourcePoolDetails.GetPoolVersionDefinition(),
                Dimensions = payload.ResourcePoolDetails.GetPoolDimensions(),
            };

            // Build core record
            var record = ResourceRecord.Build(id, time, type, location, skuName, poolReference);
            if (payload.IsAssigned)
            {
                record.IsAssigned = true;
                record.Assigned = DateTime.UtcNow;
            }

            // Update input
            payload.EntityId = id;

            // Create the actual record
            record = await ResourceRepository.CreateAsync(record, logger);

            return new ResourceRecordRef(record);
        }

        // TODO:: This needs to be called from strategy
        private async Task<AzureResourceInfo> AcquireComponentAzureResourceAsync(
          ResourceRecordRef resource,
          ResourceCreateContinuationResult resourceCreateContinuationResult,
          ResourceComponent component,
          ResourceRecordRef componentResourceReference,
          IDiagnosticsLogger logger)
        {
            if (component.ComponentType == ResourceType.OSDisk)
            {
                var computeResourceTags = new Dictionary<string, string>
                {
                    [ResourceTagName.ResourceComponentRecordIds] = componentResourceReference.Value.Id,
                };

                var diskResourceResult = await DiskProvider.AcquireOSDiskAsync(
                    new DiskProviderAcquireOSDiskInput()
                    {
                        VirtualMachineResourceInfo = resourceCreateContinuationResult.AzureResourceInfo,
                        AzureVmLocation = resource.Value.Location.ToEnum<AzureLocation>(),
                        OSDiskResourceTags = componentResourceReference.Value.GetResourceTags("QueuedAllocation"),
                        AdditionalComputeResourceTags = computeResourceTags,
                    },
                    logger.NewChildLogger());

                return diskResourceResult.AzureResourceInfo;
            }
            else
            {
                // No other case for now.
                return default;
            }
        }

        /// <summary>
        /// This is the payload that mimic the 'CreateResourceContinuationInput' type
        /// </summary>
        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : EntityContinuationJobPayloadBase<EmptyContinuationState>
        {
            public bool IsCreated { get; set; }

            /// <summary>
            /// Gets or sets the details of pool.
            /// </summary>
            [JsonConverter(typeof(ResourcePoolResourceDetailsConverter))]
            public ResourcePoolResourceDetails ResourcePoolDetails { get; set; }

            /// <summary>
            /// Gets or sets the pool type.
            /// </summary>
            [JsonConverter(typeof(StringEnumConverter))]
            public ResourceType Type { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether resource is assigned.
            /// </summary>
            public bool IsAssigned { get; set; }

            /// <summary>
            /// Gets or sets resource creation options.
            /// </summary>
            public CreateResourceContinuationInputOptions Options { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to preserve component on resource deletion.
            /// </summary>
            public bool Preserve { get; set; }

            [JsonConverter(typeof(CreateInputConverter))]
            public ContinuationInput CreateInput { get; set; }
        }

        /// <summary>
        /// Json converter for CreateInput property
        /// </summary>
        public class CreateInputConverter : JsonTypeConverter
        {
            private static readonly Dictionary<string, Type> MapTypes
                    = new Dictionary<string, Type>
                {
                    { "computeVM", typeof(VirtualMachineProviderCreateInput) },
                    { "withComponent", typeof(CreateResourceWithComponentInput) },
                    { "fileShare", typeof(FileShareProviderCreateInput) },
                    { "queue", typeof(QueueProviderCreateInput) },
                    { "keyVault", typeof(KeyVaultProviderCreateInput) },
                    { "network", typeof(NetworkInterfaceProviderCreateInput) },
                };

            protected override Type BaseType => typeof(ContinuationInput);

            protected override IDictionary<string, Type> SupportedTypes => MapTypes;
        }

        public class CreateResourceContinuationResult : EntityContinuationResult
        {
        }
    }
}
