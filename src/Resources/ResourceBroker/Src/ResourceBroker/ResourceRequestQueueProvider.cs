// <copyright file="ResourceRequestQueueProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Utils;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Provide resource allocation queues.
    /// </summary>
    public class ResourceRequestQueueProvider : IResourceRequestQueueProvider
    {
        private const string LogBase = "resource_request_queue_provider";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRequestQueueProvider"/> class.
        /// </summary>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceRepository">resource repository.</param>
        /// <param name="healthProvider">health provider.</param>
        /// <param name="resourceNameBuilder">resource name builder.</param>
        /// <param name="resourcePoolDefinitionStore">resource pool definition store.</param>
        /// <param name="controlPlaneResourceAccessor">control plane resource accessor.</param>
        public ResourceRequestQueueProvider(
            ITaskHelper taskHelper,
            ResourceBrokerSettings resourceBrokerSettings,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceRepository resourceRepository,
            IHealthProvider healthProvider,
            IResourceNameBuilder resourceNameBuilder,
            IResourcePoolDefinitionStore resourcePoolDefinitionStore,
            IControlPlaneAzureResourceAccessor controlPlaneResourceAccessor)
        {
            TaskHelper = taskHelper;
            ResourceBrokerSettings = resourceBrokerSettings;
            ClaimedDistributedLease = claimedDistributedLease;
            ResourceRepository = resourceRepository;
            HealthProvider = healthProvider;
            ResourceNameBuilder = resourceNameBuilder;
            ResourcePoolDefinitionStore = resourcePoolDefinitionStore;
            ControlPlaneResourceAccessor = controlPlaneResourceAccessor;
        }

        private ITaskHelper TaskHelper { get; }

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceRepository ResourceRepository { get; }

        private IHealthProvider HealthProvider { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private IResourcePoolDefinitionStore ResourcePoolDefinitionStore { get; }

        private Task<ReadOnlyDictionary<string, CloudQueue>> InitializeQueueTask { get; set; }

        private IControlPlaneAzureResourceAccessor ControlPlaneResourceAccessor { get; }

        /// <inheritdoc/>
        public async Task UpdatePoolQueuesAsync(IDiagnosticsLogger logger)
        {
            InitializeQueueTask = InitializePoolRequestQueues(logger);
            await InitializeQueueTask;
        }

        /// <inheritdoc/>
        public CloudQueue GetPoolQueue(string poolName, IDiagnosticsLogger logger)
        {
            if (InitializeQueueTask.Status != TaskStatus.RanToCompletion)
            {
                return default;
            }

            var queueFound = InitializeQueueTask.Result.TryGetValue(poolName, out var poolQueue);
            if (!queueFound)
            {
                logger.LogErrorWithDetail($"{LogBase}_get_pool_queue_error", $"No Request queue allocated for pool {poolName}");
                return default;
            }

            return poolQueue;
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> DeletePoolQueueAsync(QueueProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBase}_delete_pool_queue",
                async (childLogger) =>
                {
                    var storageInfo = await ControlPlaneResourceAccessor.GetStampStorageAccountForPoolQueuesAsync(logger);
                    var queueClient = GetQueueClient(storageInfo);
                    var queue = queueClient.GetQueueReference(input.QueueName);
                    await queue.DeleteIfExistsAsync();

                    return new ContinuationResult() { Status = OperationState.Succeeded };
                });
        }

        /// <inheritdoc/>
        public async Task<int> GetPendingRequestCountForPoolAsync(string poolCode, IDiagnosticsLogger logger)
        {
            var poolQueue = GetPoolQueue(poolCode, logger.NewChildLogger());
            await poolQueue.FetchAttributesAsync();
            var pendingRequestCount = poolQueue.ApproximateMessageCount;
            return pendingRequestCount ?? 0;
        }

        private Task<ReadOnlyDictionary<string, CloudQueue>> InitializePoolRequestQueues(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                 $"{LogBase}_initialization",
                 async (childLogger) =>
                 {
                     var queueStorageInfo = await ControlPlaneResourceAccessor.GetStampStorageAccountForPoolQueuesAsync(logger);

                     childLogger.FluentAddBaseValue("PoolQueueStorageAccount", queueStorageInfo.StorageAccountName)
                         .FluentAddBaseValue("PoolQueueResourceGroup", queueStorageInfo.ResourceGroup)
                         .FluentAddBaseValue("PoolQueueSubscription", queueStorageInfo.SubscriptionId);

                     var resourcePools = await ResourcePoolDefinitionStore.RetrieveDefinitionsAsync();

                     if (resourcePools == default)
                     {
                         throw new InvalidOperationException("Resource pool definition did not hydrate. Timeout occurred.");
                     }

                     var queueClient = GetQueueClient(queueStorageInfo);
                     var allocationQueues = new Dictionary<string, CloudQueue>();

                     foreach (var resourcePool in resourcePools)
                     {
                         await CreatePoolRequestQueues(childLogger, resourcePool, queueClient, allocationQueues);
                     }

                     await TaskHelper.RunEnumerableAsync(
                        $"{LogBase}_create_pool_queue_record",
                        resourcePools,
                        (resourcePool, itemLogger) => CreatePoolQueueRecordIfNotExist(queueStorageInfo, resourcePool, allocationQueues, itemLogger),
                        childLogger,
                        (resourcePool, itemLogger) => ObtainLeaseAsync($"{LogBase}-lease-{resourcePool.Details.GetPoolDefinition()}", TimeSpan.FromMinutes(10), itemLogger));

                     return new ReadOnlyDictionary<string, CloudQueue>(allocationQueues);
                 },
                 (e, childLogger) =>
                 {
                     // We cannot use the service at this point. Mark it as unhealthy to request a restart.
                     HealthProvider.MarkUnhealthy(e, logger);

                     return Task.FromResult(default(ReadOnlyDictionary<string, CloudQueue>));
                 });
        }

        private Task CreatePoolRequestQueues(IDiagnosticsLogger logger, ResourcePool resourcePool, CloudQueueClient queueClient, Dictionary<string, CloudQueue> allocationQueues)
        {
            return logger.OperationScopeAsync(
                 $"{LogBase}_create_pool_queue",
                 async (childLogger) =>
                 {
                     var poolCode = resourcePool.Details.GetPoolDefinition();
                     var queueName = ResourceNameBuilder.GetPoolQueueName(poolCode);

                     childLogger.FluentAddBaseValue("PoolQueueSku", resourcePool.Details.SkuName)
                         .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolQueueName, queueName)
                         .FluentAddBaseValue("PoolQueueLocation", resourcePool.Details.Location);

                     var queue = queueClient.GetQueueReference(queueName);
                     var newQueueCreated = await queue.CreateIfNotExistsAsync();

                     childLogger.FluentAddValue("PoolQueueExists", !newQueueCreated);

                     allocationQueues[poolCode] = queue;
                 });
        }

        private Task<IDisposable> ObtainLeaseAsync(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }

        private Task CreatePoolQueueRecordIfNotExist(QueueStorageInfo queueStorageInfo, ResourcePool resourcePool, Dictionary<string, CloudQueue> allocationQueues, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                 $"{LogBase}_create_pool_queue_record",
                 async (childLogger) =>
                 {
                     var poolCode = resourcePool.Details.GetPoolDefinition();
                     var poolQueueCode = resourcePool.Details.GetPoolDefinition().GetPoolQueueDefinition();
                     var queueName = allocationQueues[poolCode].Name;

                     childLogger.FluentAddBaseValue("PoolQueueSku", resourcePool.Details.SkuName)
                        .FluentAddBaseValue("PoolQueueName", queueName)
                        .FluentAddBaseValue("PoolQueueLocation", resourcePool.Details.Location);

                     var queueResourceInfo = new AzureResourceInfo(queueStorageInfo.SubscriptionId, queueStorageInfo.ResourceGroup, queueName)
                     {
                         Properties = new Dictionary<string, string>()
                         {
                             [AzureResourceInfoQueueDetailsProxy.StorageAccountName] = queueStorageInfo.StorageAccountName,
                             [AzureResourceInfoQueueDetailsProxy.LocationName] = resourcePool.Details.Location.ToString(),
                         },
                     };

                     var poolQueueRecord = await ResourceRepository.GetPoolQueueRecordAsync(poolQueueCode, childLogger.NewChildLogger());

                     childLogger.FluentAddBaseValue("PoolQueueRecordExists", poolQueueRecord != default);

                     if (poolQueueRecord == default)
                     {
                         poolQueueRecord = await CreatePoolQueueRecord(resourcePool, queueResourceInfo, childLogger.NewChildLogger());
                     }
                     else if (!poolQueueRecord.AzureResourceInfo.Equals(queueResourceInfo))
                     {
                         throw new InvalidOperationException($"Pool Queue record {poolQueueRecord.Id} found with different queue details.");
                     }

                     childLogger.FluentAddBaseValue("PoolQueueResourceId", poolQueueRecord.Id);
                 });
        }

        private Task<ResourceRecord> CreatePoolQueueRecord(ResourcePool pool, AzureResourceInfo queueResourceInfo, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                   $"{LogBase}_create_new_pool_queue_record",
                   async (childLogger) =>
                   {
                       var poolReference = new ResourcePoolDefinitionRecord
                       {
                           Code = pool.Details.GetPoolDefinition().GetPoolQueueDefinition(),
                           VersionCode = pool.Details.GetPoolVersionDefinition(),
                           Dimensions = pool.Details.GetPoolDimensions(),
                       };

                       var resource = ResourceRecord.Build(
                                               Guid.NewGuid(),
                                               DateTime.UtcNow,
                                               ResourceType.PoolQueue,
                                               pool.Details.Location,
                                               pool.Details.SkuName,
                                               poolReference);
                       resource.IsAssigned = true;
                       resource.Assigned = DateTime.UtcNow;
                       resource.IsReady = true;
                       resource.Ready = DateTime.UtcNow;
                       resource.ProvisioningStatus = OperationState.Succeeded;
                       resource.AzureResourceInfo = queueResourceInfo;

                       // Create the pool queue record
                       resource = await ResourceRepository.CreateAsync(resource, childLogger.NewChildLogger());
                       return resource;
                   });
        }

        private CloudQueueClient GetQueueClient(QueueStorageInfo storageInfo)
        {
            var storageCredentials = new StorageCredentials(storageInfo.StorageAccountName, storageInfo.StorageAccountKey);
            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            var queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
            return queueClient;
        }
    }
}
