// <copyright file="CreateComputeWithComponentsStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
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
        /// <param name="diskProvider">Disk provider.</param>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="creationStrategies">Resource creation strategies.</param>
        public CreateComputeWithComponentsStrategy(
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            ICapacityManager capacityManager,
            IResourceRepository resourceRepository,
            IDiskProvider diskProvider,
            IComputeProvider computeProvider,
            IEnumerable<ICreateComponentStrategy> creationStrategies)
        {
            AzureSubscriptionCatalog = Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            CapacityManager = capacityManager;
            ResourceRepository = resourceRepository;
            CreationStrategies = creationStrategies;
            DiskProvider = diskProvider;
            ComputeProvider = computeProvider;
        }

        private string LogBaseName => "create_compute_with_component_strategy";

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private ICapacityManager CapacityManager { get; }

        private IResourceRepository ResourceRepository { get; }

        private IDiskProvider DiskProvider { get; }

        private IComputeProvider ComputeProvider { get; }

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

            // Set up the selection criteria and select a subscription/location.
            var subscriptionCriteria = new List<AzureResourceCriterion>();

            if (input.Options is CreateComputeContinuationInputOptions computeOption)
            {
                resourceLocation = await HandleComputeOptionsAsync(input, resource, components, componentInputs, computeOption, subscriptionCriteria, logger);
            }

            await CreateQueueComponentIfNeededAsync(input, componentInputs, components, logger);

            if (resourceLocation == default)
            {
                // Ensure that the details type is correct
                if (!(input.ResourcePoolDetails is ResourcePoolComputeDetails computeDetails))
                {
                    throw new NotSupportedException($"Pool compute details type is not selected - {input.ResourcePoolDetails.GetType()}");
                }

                subscriptionCriteria.Add(new AzureResourceCriterion { ServiceType = ServiceType.Compute, Quota = computeDetails.SkuFamily, Required = computeDetails.Cores });

                resourceLocation = await CapacityManager.SelectAzureResourceLocation(
                    subscriptionCriteria, input.ResourcePoolDetails.Location, logger.NewChildLogger());
            }

            var result = new CreateResourceWithComponentInput
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
                            ComponentId = id,
                            AzureResourceInfo = taskResult.AzureResourceInfo,
                            ComponentType = originalInput.Input.Type,
                            Preserve = originalInput.Input.Preserve,
                            ResourceRecordId = (originalInput.Input.ResourceId != Guid.Empty) ? originalInput.Input.ResourceId.ToString() : default,
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
                NextInput = input.OperationInput,
                RetryAfter = TimeSpan.FromSeconds(1),
                Status = OperationState.InProgress,
                Components = new ResourceComponentDetail()
                {
                    Items = operationInput.CustomComponents.ToComponentDictionary(),
                },
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

            if (resourceResult.Status == OperationState.Succeeded)
            {
                // Copy over queue component to OS disk.
                if (input.Options is CreateComputeContinuationInputOptions computeOption)
                {
                    if (computeOption.OSDiskResourceId != default)
                    {
                        await logger.RetryOperationScopeAsync(
                           $"{LogBaseName}_record_update",
                           async (IDiagnosticsLogger innerLogger) =>
                           {
                               var existingOSDisk = await ResourceRepository.GetAsync(computeOption.OSDiskResourceId, logger.NewChildLogger());

                               if (existingOSDisk.Components == default)
                               {
                                   existingOSDisk.Components = new ResourceComponentDetail();
                               }

                               if (existingOSDisk.Components.Items == default)
                               {
                                   existingOSDisk.Components.Items = new Dictionary<string, ResourceComponent>();
                               }

                               var queueComponentInOSDisk = existingOSDisk.Components.Items.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue).Value;
                               var queueComponentInCompute = resource.Value.Components.Items.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue).Value;

                               if (queueComponentInOSDisk != default)
                               {
                                   existingOSDisk.Components.Items.Remove(queueComponentInOSDisk.ComponentId);
                               }

                               if (queueComponentInCompute != default)
                               {
                                   existingOSDisk.Components.Items.Add(queueComponentInCompute.ComponentId, queueComponentInCompute);
                               }

                               await ResourceRepository.UpdateAsync(existingOSDisk, logger.NewChildLogger());
                           });
                    }
                    else if (computeOption.CreateOSDiskRecord && input.ResourcePoolDetails is ResourcePoolComputeDetails resourcePoolComputeDetails)
                    {
                        // Windows hotpool.
                        var osDiskResource = await CreateOSDiskRecord(resourcePoolComputeDetails, logger.NewChildLogger());
                        var computeResource = resource.Value;
                        var computeResourceTags = new Dictionary<string, string>
                        {
                            [ResourceTagName.ResourceComponentRecordIds] = osDiskResource.Id,
                        };

                        // Acquire OS disk information.
                        var diskResourceResult = await DiskProvider.AcquireOSDiskAsync(
                            new DiskProviderAcquireOSDiskInput()
                            {
                                VirtualMachineResourceInfo = computeResource.AzureResourceInfo,
                                AzureVmLocation = computeResource.Location.ToEnum<AzureLocation>(),
                                OSDiskResourceTags = osDiskResource.GetResourceTags(input.Reason),
                                AdditionalComputeResourceTags = computeResourceTags,
                            },
                            logger.NewChildLogger());

                        await logger.RetryOperationScopeAsync(
                                $"{LogBaseName}_osdisk_record_update",
                                async (IDiagnosticsLogger innerLogger) =>
                                {
                                    // Update disk azure resource info.
                                    osDiskResource.AzureResourceInfo = diskResourceResult.AzureResourceInfo;

                                    // Copy queue info.
                                    osDiskResource = CloneQueueAndCopyComponent(computeResource, osDiskResource);

                                    // Copy provisioning and ready status.
                                    osDiskResource.ProvisioningReason = computeResource.ProvisioningReason;
                                    osDiskResource.ProvisioningStatus = computeResource.ProvisioningStatus;
                                    osDiskResource.IsReady = computeResource.IsReady;
                                    osDiskResource.Ready = computeResource.Ready;

                                    osDiskResource = await ResourceRepository.UpdateAsync(osDiskResource, logger.NewChildLogger());
                                });

                        // Update compute record with disk components
                        await logger.RetryOperationScopeAsync(
                                $"{LogBaseName}_compute_record_update",
                                async (IDiagnosticsLogger innerLogger) =>
                                {
                                    computeResource = await ResourceRepository.GetAsync(computeResource.Id, innerLogger.NewChildLogger());

                                    computeResource.Components.Items[osDiskResource.Id] = new ResourceComponent(
                                                                                                ResourceType.OSDisk,
                                                                                                osDiskResource.AzureResourceInfo,
                                                                                                osDiskResource.Id,
                                                                                                preserve: true);

                                    var queueComponent = computeResource.Components.Items.Single(x => x.Value.ComponentType == ResourceType.InputQueue);
                                    queueComponent.Value.Preserve = true;

                                    computeResource = await ResourceRepository.UpdateAsync(computeResource, innerLogger.NewChildLogger());
                                });

                        // Updates ComputeVM tags to include OS Disk record id.
                        await ComputeProvider.UpdateTagsAsync(
                           new VirtualMachineProviderUpdateTagsInput()
                           {
                               VirtualMachineResourceInfo = computeResource.AzureResourceInfo,
                               CustomComponents = computeResource.Components?.Items?.Values.ToList(),
                               AdditionalComputeResourceTags = computeResourceTags,
                           },
                           logger.NewChildLogger());
                    }
                }
            }

            return resourceResult;
        }

        private async Task<ResourceRecord> CreateOSDiskRecord(ResourcePoolComputeDetails details, IDiagnosticsLogger logger)
        {
            var resource = details.CreateOSDiskRecord();
            resource.IsAssigned = true;
            resource.Assigned = DateTime.UtcNow;

            // Create the actual record
            resource = await ResourceRepository.CreateAsync(resource, logger.NewChildLogger());
            return resource;
        }

        private ResourceRecord CloneQueueAndCopyComponent(ResourceRecord sourceRecord, ResourceRecord targetRecord)
        {
            var queueComponent = sourceRecord.Components?.Items.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue);

            if (!queueComponent.Value.Value.Preserve)
            {
                queueComponent.Value.Value.Preserve = true;
            }

            if (targetRecord.Components == default)
            {
                targetRecord.Components = new ResourceComponentDetail();
            }

            if (targetRecord.Components.Items == default)
            {
                targetRecord.Components.Items = new Dictionary<string, ResourceComponent>();
            }

            if (queueComponent.HasValue)
            {
                targetRecord.Components.Items[queueComponent.Value.Key] = queueComponent.Value.Value;
            }

            return targetRecord;
        }

        private async Task CreateQueueComponentIfNeededAsync(
            CreateResourceContinuationInput input,
            Dictionary<string, ComponentInput> componentInputs,
            List<ResourceComponent> components,
            IDiagnosticsLogger logger)
        {
            var queueComponent = components?.SingleOrDefault(x => x.ComponentType == ResourceType.InputQueue);
            if (queueComponent != default)
            {
                return;
            }

            var osDiskComponent = components?.SingleOrDefault(x => x.ComponentType == ResourceType.OSDisk);

            var preserve = osDiskComponent != default;

            // Create an input queue.
            var queueInput = new CreateResourceContinuationInput()
            {
                Type = ResourceType.InputQueue,
                ResourcePoolDetails = input.ResourcePoolDetails,
                Reason = input.Reason,
                Options = input.Options,
                IsAssigned = true,
                Preserve = preserve,
            };

            var queueStrategy = CreationStrategies.Where(s => s.CanHandle(queueInput)).Single();
            queueInput.OperationInput = await queueStrategy.BuildCreateOperationInputAsync(queueInput, default, logger.NewChildLogger());
            var componentInput = new ComponentInput()
            {
                ComponentId = Guid.NewGuid().ToString(),
                Input = queueInput,
                Status = OperationState.NotStarted,
            };

            componentInputs[componentInput.ComponentId] = componentInput;
        }

        private async Task<IAzureResourceLocation> HandleComputeOptionsAsync(
            CreateResourceContinuationInput input,
            ResourceRecordRef resource,
            List<ResourceComponent> components,
            Dictionary<string, ComponentInput> componentInputs,
            CreateComputeContinuationInputOptions computeOption,
            List<AzureResourceCriterion> subscriptionCriteria,
            IDiagnosticsLogger logger)
        {
            var osDiskId = computeOption.OSDiskResourceId;
            var existingOSDisk = default(ResourceRecord);
            var resourceLocation = default(IAzureResourceLocation);
            var preserveDisk = osDiskId != default;

            if (preserveDisk)
            {
                existingOSDisk = await ResourceRepository.GetAsync(osDiskId, logger.NewChildLogger());
                components.Add(new ResourceComponent(ResourceType.OSDisk, existingOSDisk.AzureResourceInfo, osDiskId, preserve: preserveDisk));
            }

            var queueComponent = default(ResourceComponent);

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

                queueComponent = existingOSDisk.Components?.Items?.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue).Value;
            }

            if (queueComponent != default)
            {
                // Input queue exists.
                components.Add(queueComponent);
            }

            var networkCriteria = new AzureResourceCriterion { ServiceType = ServiceType.Network, Quota = "VirtualNetworks", Required = 1 };

            if (computeOption.SeparateNetworkAndComputeSubscriptions || !string.IsNullOrEmpty(computeOption.SubnetResourceId))
            {
                Guid networkSubscriptionId;
                string networkResourceGroup;
                if (!string.IsNullOrEmpty(computeOption.SubnetResourceId))
                {
                    // TODO:: Check for network interface capacity in subnetSubscription (which is owned by the user)
                    var subnetSubscription = ResourceId.FromString(computeOption.SubnetResourceId)?.SubscriptionId;
                    if (string.IsNullOrEmpty(subnetSubscription) || !Guid.TryParse(subnetSubscription, out networkSubscriptionId))
                    {
                        throw new NotSupportedException($"Subnet resource id is not valid - {computeOption.SubnetResourceId}");
                    }

                    var subnetResourceGroup = ResourceId.FromString(computeOption.SubnetResourceId)?.ResourceGroupName;
                    if (string.IsNullOrEmpty(subnetResourceGroup))
                    {
                        throw new NotSupportedException($"Subnet resource id is not valid - {computeOption.SubnetResourceId}");
                    }

                    networkResourceGroup = subnetResourceGroup;
                }
                else
                {
                    var networkSubscription = await CapacityManager.SelectAzureResourceLocation(
                        new[] { networkCriteria }, input.ResourcePoolDetails.Location, logger.NewChildLogger());

                    networkSubscriptionId = Guid.Parse(networkSubscription.Subscription.SubscriptionId);
                    networkResourceGroup = networkSubscription.ResourceGroup;
                }

                var nicInput = new CreateNetworkInterfaceContinuationInput()
                {
                    Type = ResourceType.NetworkInterface,
                    ResourcePoolDetails = input.ResourcePoolDetails,
                    Reason = input.Reason,
                    Options = input.Options,
                    IsAssigned = true,
                    SubscriptionId = networkSubscriptionId,
                    ResourceGroup = networkResourceGroup,
                };
                var nicStrategy = CreationStrategies.Where(s => s.CanHandle(nicInput)).Single();
                nicInput.OperationInput = await nicStrategy.BuildCreateOperationInputAsync(nicInput, resource, logger.NewChildLogger());
                var componentInput = new ComponentInput()
                {
                    ComponentId = Guid.NewGuid().ToString(),
                    Input = nicInput,
                    Status = OperationState.NotStarted,
                };
                componentInputs[componentInput.ComponentId] = componentInput;
            }
            else
            {
                // Legacy behavior where vnet and VM go into the same subscription - add the criteria for the vnet
                subscriptionCriteria.Add(networkCriteria);
            }

            return resourceLocation;
        }
    }
}