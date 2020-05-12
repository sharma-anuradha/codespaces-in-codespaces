// <copyright file="CreateComputeWithComponentsStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Create compute with custom components.
    /// </summary>
    public class CreateComputeWithComponentsStrategy : ICreateResourceStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateComputeWithComponentsStrategy"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">Azure subscription catalog.</param>
        /// <param name="capacityManager">The capacity manager.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="creationStrategies">Resource creation strategies.</param>
        public CreateComputeWithComponentsStrategy(
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            ICapacityManager capacityManager,
            IResourceRepository resourceRepository,
            IEnumerable<ICreateComponentStrategy> creationStrategies)
        {
            AzureSubscriptionCatalog = Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            CapacityManager = capacityManager;
            ResourceRepository = resourceRepository;
            CreationStrategies = creationStrategies;
        }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private ICapacityManager CapacityManager { get; }

        private IResourceRepository ResourceRepository { get; }

        private IEnumerable<ICreateComponentStrategy> CreationStrategies { get; }

        /// <inheritdoc/>
        public bool CanHandle(CreateResourceContinuationInput input)
        {
            return input.Type == ResourceType.ComputeVM;
        }

        /// <inheritdoc/>
        public async Task<ContinuationInput> BuildCreateOperationInputAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            // Base resource tags that will be attached
            var resourceTags = resource.Value.GetResourceTags(input.Reason);

            if (resource.Value.Type != ResourceType.ComputeVM)
            {
                throw new NotSupportedException($"Resource type is not supported - {resource.Value.Type}");
            }

            var components = new List<ResourceComponent>();
            var componentInputs = new Dictionary<string, ComponentInput>();
            var resourceLocation = default(IAzureResourceLocation);

            if (input.Options is CreateComputeContinuationInputOptions computeOption)
            {
                resourceLocation = await HandleComputeOptions(input, components, componentInputs, computeOption, logger);
            }

            if (resourceLocation == default)
            {
                // Ensure that the details type is correct
                if (!(input.ResourcePoolDetails is ResourcePoolComputeDetails computeDetails))
                {
                    throw new NotSupportedException($"Pool compute details type is not selected - {input.ResourcePoolDetails.GetType()}");
                }

                // Set up the selection criteria and select a subscription/location.
                var criteria = new List<AzureResourceCriterion>
                        {
                            // SkuFamily must be first as the primary criterion for ordering candidate subscriptions.
                            new AzureResourceCriterion { ServiceType = ServiceType.Compute, Quota = computeDetails.SkuFamily, Required = computeDetails.Cores },
                            new AzureResourceCriterion { ServiceType = ServiceType.Network, Quota = "VirtualNetworks", Required = 1 },
                        };

                resourceLocation = await CapacityManager.SelectAzureResourceLocation(
                    criteria, input.ResourcePoolDetails.Location, logger.NewChildLogger());
            }

            ContinuationInput result = new CreateResourceWithComponentInput
            {
                ResourceId = resource.Value.Id,
                AzureSkuName = input.ResourcePoolDetails.SkuName,
                AzureSubscription = Guid.Parse(resourceLocation.Subscription.SubscriptionId),
                AzureResourceGroup = resourceLocation.ResourceGroup,
                ResourceTags = resourceTags,
                CustomComponents = components,
                CustomComponentInputs = componentInputs,
                Stage = componentInputs.Count == 0 ? ResourceCreationState.CreateResource : ResourceCreationState.CreateComponent,
            };

            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<ResourceCreateContinuationResult> RunCreateOperationCoreAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            if (input.OperationInput is CreateResourceWithComponentInput operationInput)
            {
                switch (operationInput.Stage)
                {
                    case ResourceCreationState.CreateComponent:
                        return await CreateComponentAsync(input, resource, logger);
                    case ResourceCreationState.CreateResource:
                        return await CreateResourceAsync(input, resource, logger);
                    default:
                        throw new NotSupportedException($"Resource creation stage is not supported - {operationInput.Stage}");
                }
            }
            else if (input.OperationInput is VirtualMachineProviderCreateInput computeInput)
            {
                return await RunCheckResourceProvisioning(input, resource, logger);
            }
            else
            {
                throw new NotSupportedException($"Resource input type is not supported.");
            }
        }

        private async Task<ResourceCreateContinuationResult> CreateComponentAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            ResourceCreateContinuationResult result;
            var componentCreateTasks = new List<(Task<ResourceCreateContinuationResult> task, string id)>();
            var operationInput = (CreateResourceWithComponentInput)input.OperationInput;

            // Run create operation
            foreach (var componentInput in operationInput.CustomComponentInputs.Values)
            {
                if (!componentInput.Status.IsFinal())
                {
                    var strategy = CreationStrategies.Where(s => s.CanHandle(componentInput.Input)).Single();
                    componentCreateTasks.Add((strategy.RunCreateOperationCoreAsync(componentInput.Input, resource, logger.NewChildLogger()), componentInput.ComponentId));
                }
            }

            await Task.WhenAll(componentCreateTasks.Select(t => t.task));
            if (componentCreateTasks.Any(item
                 => item.task.Result.Status == OperationState.Failed
                 || item.task.Result.Status == OperationState.Cancelled))
            {
                // Add logging for failed task.
                return new ResourceCreateContinuationResult()
                {
                    ErrorReason = "ComponentCreationFailed",
                    Status = OperationState.Failed,
                };
            }

            var stage = ResourceCreationState.CreateResource;

            foreach (var (task, id) in componentCreateTasks)
            {
                var taskResult = task.Result;
                var taskInput = taskResult.NextInput;
                var originalInput = operationInput.CustomComponentInputs[id];
                var newInput = originalInput;
                newInput.Status = taskResult.Status;

                if (taskResult.Status == OperationState.Succeeded)
                {
                    operationInput.CustomComponents.Add(
                            new ResourceComponent()
                            {
                                AzureResourceInfo = taskResult.AzureResourceInfo,
                                ComponentType = originalInput.Input.Type,
                                Preserve = originalInput.Input.Preserve,
                                ResourceRecordId = originalInput.Input.ResourceId.ToString(),
                            });
                }
                else if (taskResult.Status == OperationState.InProgress)
                {
                    newInput.Input.OperationInput = taskResult.NextInput;
                    newInput.Status = taskResult.Status;
                    stage = ResourceCreationState.CreateComponent;
                }

                operationInput.CustomComponentInputs[id] = newInput;
            }

            operationInput.Stage = stage;
            input.OperationInput = operationInput;

            result = new ResourceCreateContinuationResult()
            {
                NextInput = input,
                RetryAfter = TimeSpan.FromSeconds(1),
                Status = OperationState.InProgress,
            };

            return result;
        }

        private async Task<ResourceCreateContinuationResult> CreateResourceAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var resourceResult = (ResourceCreateContinuationResult)default;

            // Run create operation
            var resourceInput = new CreateResourceContinuationInput()
            {
                Type = input.Type,
                ResourcePoolDetails = input.ResourcePoolDetails,
                Reason = input.Reason,
                OperationInput = input.OperationInput,
                IsAssigned = input.IsAssigned,
            };

            var resourceStrategy = CreationStrategies.Where(s => s.CanHandle(input)).Single();
            resourceInput.OperationInput = await resourceStrategy.BuildCreateOperationInputAsync(input, resource, logger.NewChildLogger());

            resourceResult = await resourceStrategy.RunCreateOperationCoreAsync(resourceInput, resource, logger.NewChildLogger());

            return resourceResult;
        }

        private async Task<ResourceCreateContinuationResult> RunCheckResourceProvisioning(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var resourceStrategy = CreationStrategies.Where(s => s.CanHandle(input)).Single();
            var resourceResult = await resourceStrategy.RunCreateOperationCoreAsync(input, resource, logger.NewChildLogger());

            return resourceResult;
        }

        private async Task<IAzureResourceLocation> HandleComputeOptions(
            CreateResourceContinuationInput input,
            List<ResourceComponent> components,
            Dictionary<string, ComponentInput> componentInputs,
            CreateComputeContinuationInputOptions computeOption,
            IDiagnosticsLogger logger)
        {
            var osDiskId = computeOption.OSDiskResourceId;
            var existingOSDisk = default(ResourceRecord);
            var resourceLocation = default(IAzureResourceLocation);

            if (osDiskId != default)
            {
                existingOSDisk = await ResourceRepository.GetAsync(osDiskId, logger.NewChildLogger());
                components.Add(new ResourceComponent(ResourceType.OSDisk, existingOSDisk.AzureResourceInfo, osDiskId, preserve: true));
            }

            if (existingOSDisk?.AzureResourceInfo?.Name != default)
            {
                // Creates VM with an already existing resource.
                // TODO: janraj, this overrides the criteria based selection, because the OSDisk is still in the same place it was originally created. Future WI.
                // TODO: janraj, copy disk to target subscription and create VM. copying azure managed disks takes ~10 seconds.
                var azureSubscription = AzureSubscriptionCatalog.AzureSubscriptions.Single(x => x.SubscriptionId == existingOSDisk.AzureResourceInfo.SubscriptionId.ToString());
                var azureLocation = existingOSDisk.Location.ToEnum<AzureLocation>();

                resourceLocation = new AzureResourceLocation(
                    azureSubscription,
                    existingOSDisk.AzureResourceInfo.ResourceGroup,
                    azureLocation);
            }

            if (computeOption.SubnetResourceInfo != default)
            {
                var nicInput = new CreateResourceContinuationInput()
                {
                    Type = ResourceType.NetworkInterface,
                    ResourcePoolDetails = input.ResourcePoolDetails,
                    Reason = input.Reason,
                    Options = input.Options,
                    IsAssigned = true,
                };
                var nicStrategy = CreationStrategies.Where(s => s.CanHandle(nicInput)).Single();
                nicInput.OperationInput = await nicStrategy.BuildCreateOperationInputAsync(nicInput, default, logger.NewChildLogger());
                var componentInput = new ComponentInput()
                {
                    ComponentId = Guid.NewGuid().ToString(),
                    Input = nicInput,
                    Status = OperationState.NotStarted,
                };
                componentInputs[componentInput.ComponentId] = componentInput;
            }

            return resourceLocation;
        }
    }
}