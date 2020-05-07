// <copyright file="CreateResourceContinuationHandlerV2.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages creating of environment.
    /// </summary>
    public class CreateResourceContinuationHandlerV2
        : BaseContinuationTaskMessageHandler<CreateResourceContinuationInput>, ICreateResourceContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobCreateResourceV2";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateResourceContinuationHandlerV2"/> class.
        /// </summary>
        /// <param name="diskProvider">The disk provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="creationStrategies">Disk provider.</param>
        public CreateResourceContinuationHandlerV2(
            IDiskProvider diskProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider,
            IEnumerable<ICreateResourceStrategy> creationStrategies)
            : base(serviceProvider, resourceRepository)
        {
            CreationStrategies = creationStrategies;
            DiskProvider = diskProvider;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerCreate;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Provisioning;

        /// <summary>
        /// Gets resource creation strategies.
        /// </summary>
        protected IEnumerable<ICreateResourceStrategy> CreationStrategies { get; }

        private IDiskProvider DiskProvider { get; }

        /// <inheritdoc/>
        protected override async Task<ResourceRecordRef> FetchReferenceAsync(CreateResourceContinuationInput input, IDiagnosticsLogger logger)
        {
            // If we have a reference use that
            if (string.IsNullOrEmpty(input.ContinuationToken))
            {
                return await CreateReferenceAsync(input, logger);
            }

            return await FetchReferenceAsync(input.ResourceId, logger);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationInput> BuildOperationInputAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var createStrategy = CreationStrategies.Where(s => s.CanHandle(input)).Single();

            return await createStrategy.BuildCreateOperationInputAsync(input, resource, logger);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var createStrategy = CreationStrategies.Where(s => s.CanHandle(input)).Single();

            var result = await createStrategy.RunCreateOperationCoreAsync(input, resource, logger);

            // Make sure we bring over the Resource info if we have it
            if (result.AzureResourceInfo != default)
            {
                // Update the components provisioning status.
                if (result.Status == OperationState.Succeeded && result.Components.Items != default)
                {
                    foreach (var component in result.Components.Items.Values)
                    {
                        if (component.ResourceRecordId != default)
                        {
                            var componentResourceReference = await FetchReferenceAsync(Guid.Parse(component.ResourceRecordId), logger.NewChildLogger());

                            if (component.AzureResourceInfo == default)
                            {
                                component.AzureResourceInfo = await AcquireComponentAzureResourceAsync(resource, result, logger, component, componentResourceReference);

                                if (componentResourceReference.Value.ProvisioningStatus != result.Status ||
                                    !component.AzureResourceInfo.Equals(componentResourceReference.Value.AzureResourceInfo))
                                {
                                    await UpdateRecordAsync(
                                        input,
                                        componentResourceReference,
                                        (componentRecord, childLogger) =>
                                        {
                                            componentRecord.ProvisioningStatus = result.Status;
                                            componentRecord.ProvisioningStatusChanged = DateTime.UtcNow;
                                            componentRecord.AzureResourceInfo = component.AzureResourceInfo;

                                            return true;
                                        },
                                        logger.NewChildLogger());
                                }
                            }
                        }
                    }
                }

                if (resource.Value.AzureResourceInfo == default
                    || !resource.Value.AzureResourceInfo.Equals(result.AzureResourceInfo)
                    || !resource.Value.Components.Equals(result.Components))
                {
                    // Retry till we succeed
                    await logger.RetryOperationScopeAsync(
                        $"{LogBaseName}_record_update",
                        async (IDiagnosticsLogger innerLogger) =>
                        {
                            resource.Value = (await FetchReferenceAsync(input, innerLogger)).Value;

                            resource.Value.AzureResourceInfo = result.AzureResourceInfo;

                            resource.Value.Components = result.Components;

                            resource.Value = await ResourceRepository.UpdateAsync(resource.Value, innerLogger.NewChildLogger());
                        });
                }
            }

            return result;
        }

        private async Task<ResourceRecordRef> CreateReferenceAsync(CreateResourceContinuationInput input, IDiagnosticsLogger logger)
        {
            // Common properties
            var id = input.ResourceId;
            var time = DateTime.UtcNow;
            var type = input.Type;
            var location = input.ResourcePoolDetails.Location;
            var skuName = input.ResourcePoolDetails.SkuName;
            var poolReference = new ResourcePoolDefinitionRecord
            {
                Code = input.ResourcePoolDetails.GetPoolDefinition(),
                VersionCode = input.ResourcePoolDetails.GetPoolVersionDefinition(),
                Dimensions = input.ResourcePoolDetails.GetPoolDimensions(),
            };

            // Build core record
            var record = ResourceRecord.Build(id, time, type, location, skuName, poolReference);
            if (input.IsAssigned)
            {
                record.IsAssigned = true;
                record.Assigned = DateTime.UtcNow;
            }

            // Update input
            input.ResourceId = id;

            // Create the actual record
            record = await ResourceRepository.CreateAsync(record, logger);

            return new ResourceRecordRef(record);
        }

        // TODO:: This needs to be called from strategy
        private async Task<AzureResourceInfo> AcquireComponentAzureResourceAsync(
          ResourceRecordRef resource,
          ResourceCreateContinuationResult resourceCreateContinuationResult,
          IDiagnosticsLogger logger,
          ResourceComponent component,
          ResourceRecordRef componentResourceReference)
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
    }
}
