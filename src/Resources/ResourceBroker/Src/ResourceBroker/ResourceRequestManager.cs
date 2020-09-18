// <copyright file="ResourceRequestManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Fulfills resource requests on first come first serve basis.
    /// </summary>
    public class ResourceRequestManager : IResourceRequestManager
    {
        private const string LogBaseName = "resource_request_manager";
        private const string QueueResourceRequestEnabled = "QueueResourceRequestEnabled";
        private const string FeatureFlagKey = "featureflag:queue-resource-request-enabled";
        private const bool FeatureFlagDefault = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRequestManager"/> class.
        /// </summary>
        /// <param name="resourceRequestQueueProvider">Resource request queue provider.</param>
        /// <param name="resourceRepository">Resource Repository.</param>
        /// <param name="systemConfiguration">System configuration settings.</param>
        public ResourceRequestManager(
            IResourceRequestQueueProvider resourceRequestQueueProvider,
            IResourceRepository resourceRepository,
            ISystemConfiguration systemConfiguration)
        {
            ResourceRequestQueueProvider = Requires.NotNull(resourceRequestQueueProvider, nameof(resourceRequestQueueProvider));
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            SystemConfiguration = Requires.NotNull(systemConfiguration, nameof(systemConfiguration));
        }

        private ISystemConfiguration SystemConfiguration { get; }

        private IResourceRequestQueueProvider ResourceRequestQueueProvider { get; }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        public Task<ResourceRecord> EnqueueAsync(ResourcePool resourcePool, IDictionary<string, string> loggingProperties, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
               $"{LogBaseName}_enqueue_request",
               async (childLogger) =>
               {
                   var queueResourceRequest = await SystemConfiguration.GetValueAsync(FeatureFlagKey, childLogger.NewChildLogger(), FeatureFlagDefault);

                   childLogger.FluentAddBaseValues(loggingProperties)
                       .FluentAddBaseValue(nameof(resourcePool.Details.SkuName), resourcePool.Details.SkuName)
                       .FluentAddBaseValue(QueueResourceRequestEnabled, queueResourceRequest);

                   if (!queueResourceRequest)
                   {
                       return default;
                   }

                   var resource = await CreateShadowRecord(resourcePool, logger);

                   childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.RequestRecordId, resource.Id);

                   var poolCode = resourcePool.Details.GetPoolDefinition();
                   var poolQueue = await ResourceRequestQueueProvider.GetPoolQueueAsync(poolCode, logger.NewChildLogger());

                   childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolQueueName, poolQueue.Name);

                   var content = new CloudQueueMessage(
                                       JsonConvert.SerializeObject(
                                           new ResourceRequestQueueMessage(resource.Id, loggingProperties)));
                   await poolQueue.AddMessageAsync(content);

                   return resource;
               });
        }

        /// <inheritdoc/>
        public Task<ResourceRecord> TryAssignAsync(ResourceRecord resource, string reason, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
               $"{LogBaseName}_try_assign",
               async (childLogger) =>
               {
                   var queueResourceRequest = await SystemConfiguration.GetValueAsync(FeatureFlagKey, childLogger.NewChildLogger(), FeatureFlagDefault);

                   childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolSkuName, resource.SkuName)
                       .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolLocation, resource.Location)
                       .FluentAddBaseValue(LoggingConstants.Reason, reason)
                       .FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resource.Id)
                       .FluentAddBaseValue(QueueResourceRequestEnabled, queueResourceRequest);

                   if (!queueResourceRequest)
                   {
                       return resource;
                   }

                   var poolCode = resource.PoolReference?.Code;

                   // check if there is any request for pool definition
                   var poolQueue = await ResourceRequestQueueProvider.GetPoolQueueAsync(poolCode, logger.NewChildLogger());
                   if (poolQueue == default)
                   {
                       // Queue initialization is not complete, so do nothing.
                       return resource;
                   }

                   var message = await poolQueue.GetMessageAsync();
                   var messageProcessed = 0;
                   var retryAssignRequest = true;

                   while (message != null && retryAssignRequest)
                   {
                       var result = await TryAssignRequestAsync(message, resource, childLogger.NewChildLogger());

                       messageProcessed++;
                       resource = result.UpdatedResource;
                       retryAssignRequest = result.RetryAssignToNewRequest;

                       await poolQueue.DeleteMessageAsync(message);

                       // If Resource is assigned, then return updated resource
                       // Otherwise try to assign it to next request in queue.
                       if (retryAssignRequest)
                       {
                           // Try to assign resource to the next request.
                           message = await poolQueue.GetMessageAsync();
                       }
                   }

                   childLogger.FluentAddBaseValue(nameof(resource.IsAssigned), resource.IsAssigned)
                        .FluentAddBaseValue("RequestsProcessed", messageProcessed);

                   return resource;
               });
        }

        private Task<(bool RetryAssignToNewRequest, ResourceRecord UpdatedResource)> TryAssignRequestAsync(CloudQueueMessage message, ResourceRecord resource, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeWithCustomExceptionHandlingAsync(
                $"{LogBaseName}_try_assign_request",
                async (childLogger) =>
                {
                    var result = (true, resource);

                    // Request Found
                    var request = JsonConvert.DeserializeObject<ResourceRequestQueueMessage>(message.AsString);
                    if (request == default)
                    {
                        childLogger.LogErrorWithDetail($"{LogBaseName}_invalid_message_error", "Dequeued message can not be deserialized");

                        return result;
                    }

                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.RequestRecordId, request.RequestRecordId)
                        .FluentAddBaseValues(request.LoggingProperties);

                    // Get resource record for queued request
                    var queuedResourceRecord = await ResourceRepository.GetAsync(request.RequestRecordId, childLogger.NewChildLogger());

                    childLogger.FluentAddBaseValue("RequestCancelled", queuedResourceRecord == default)
                        .FluentAddBaseValue("RequestAlreadyAssigned", queuedResourceRecord?.AssignedResourceId != default);

                    if (queuedResourceRecord != default && queuedResourceRecord.AssignedResourceId == default)
                    {
                        // if null, Reserve Resource and Assign to request.
                        var resourceReserved = await childLogger.RetryOperationScopeAsync(
                            $"{LogBaseName}_reserve_resource",
                            async (IDiagnosticsLogger innerLogger) =>
                            {
                                // Update core properties to indicate that its assigned
                                resource.IsAssigned = true;
                                resource.Assigned = DateTime.UtcNow;
                                resource = await ResourceRepository.UpdateAsync(resource, innerLogger.NewChildLogger());
                                return true;
                            });

                        childLogger.FluentAddBaseValue("ResourceReserved", resourceReserved);

                        if (resourceReserved)
                        {
                            await childLogger.RetryOperationScopeAsync(
                                $"{LogBaseName}_assign_resource",
                                async (IDiagnosticsLogger innerLogger) =>
                                {
                                    queuedResourceRecord.AssignedResourceId = resource.Id;
                                    queuedResourceRecord = await ResourceRepository.UpdateAsync(queuedResourceRecord, innerLogger.NewChildLogger());

                                    // Update OS Disk records if needed.
                                    if (queuedResourceRecord.Type == ResourceType.ComputeVM)
                                    {
                                        var queuedOsDiskRecordId = queuedResourceRecord.GetComputeDetails().OSDiskRecordId;
                                        if (queuedOsDiskRecordId != default)
                                        {
                                            var queuedResourceComponentRecord = await ResourceRepository.GetAsync(queuedOsDiskRecordId, childLogger.NewChildLogger());
                                            queuedResourceComponentRecord.AssignedResourceId = resource.GetComputeDetails().OSDiskRecordId;
                                            await ResourceRepository.UpdateAsync(queuedResourceComponentRecord, innerLogger.NewChildLogger());
                                        }
                                    }
                                });
                        }

                        // Could not reserve resource, so stop trying to assign this resource.
                        result = (false, resource);
                    }

                    childLogger.FluentAddBaseValue("RetryAssignToNewRequest", result.Item1)
                                        .FluentAddBaseValue("RequestAssigned", queuedResourceRecord?.AssignedResourceId != default);

                    return result;
                },
                (ex, childLogger) =>
                {
                    if (ex is JsonReaderException)
                    {
                        return (true, Task.FromResult((true, resource)));
                    }

                    return (false, Task.FromResult((true, resource)));
                });
        }

        private async Task<ResourceRecord> CreateShadowRecord(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            // Core recrod
            var poolReference = new ResourcePoolDefinitionRecord()
            {
                Code = resourcePool.Details.GetPoolDefinition(),
                Dimensions = resourcePool.Details.GetPoolDimensions(),
            };

            var resource = ResourceRecord.Build(
                Guid.NewGuid(),
                DateTime.UtcNow,
                resourcePool.Type,
                resourcePool.Details.Location,
                resourcePool.Details.SkuName,
                poolReference);

            resource.IsAssigned = true;
            resource.Assigned = DateTime.UtcNow;
            resource.IsReady = false;
            resource.ProvisioningStatus = OperationState.Initialized;
            resource.ProvisioningReason = "shadow_record";

            if (resourcePool.Details is ResourcePoolComputeDetails computeDetails && computeDetails.OS == ComputeOS.Windows)
            {
                var osDiskResource = computeDetails.CreateOSDiskRecord();
                osDiskResource.IsAssigned = true;
                osDiskResource.Assigned = DateTime.UtcNow;
                var componentId = Guid.NewGuid().ToString();

                resource.Components = new ResourceComponentDetail()
                {
                    Items = new Dictionary<string, ResourceComponent>()
                    {
                        {
                            componentId,
                            new ResourceComponent()
                            {
                                    ResourceRecordId = osDiskResource.Id,
                                    ComponentId = componentId,
                                    ComponentType = ResourceType.OSDisk,
                            }
                        },
                    },
                };

                await ResourceRepository.CreateAsync(osDiskResource, logger.NewChildLogger());
            }

            // Create the actual record
            resource = await ResourceRepository.CreateAsync(resource, logger.NewChildLogger());

            return resource;
        }
    }
}