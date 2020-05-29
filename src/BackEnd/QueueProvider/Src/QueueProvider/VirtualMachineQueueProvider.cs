// <copyright file="VirtualMachineQueueProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ContainerRegistry.Fluent.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider
{
    /// <summary>
    /// Manages virtual machine queue.
    /// </summary>
    public class VirtualMachineQueueProvider : IQueueProvider
    {
        private const string LogBase = "queue_provider";

        private const int SASTokenValidityInDays = 365;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineQueueProvider"/> class.
        /// </summary>
        /// <param name="controlPlaneAzureResourceAccessor"> control plane resource accessor.</param>
        public VirtualMachineQueueProvider(IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            ControlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
        }

        private IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; }

        /// <inheritdoc/>
        public async Task<QueueProviderCreateResult> CreateAsync(
            QueueProviderCreateInput input,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
               $"{LogBase}_create",
               async (childLogger) =>
               {
                   var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(input.AzureLocation, logger);
                   var queue = GetQueueClient(storageInfo, input.QueueName, logger);
                   var queueCreated = await queue.CreateIfNotExistsAsync();

                   if (!queueCreated)
                   {
                       return new QueueProviderCreateResult()
                       {
                           ErrorReason = $"Failed to create queue {input.QueueName}",
                           Status = OperationState.Failed,
                       };
                   }

                   var azureResourceInfo = new AzureResourceInfo(storageInfo.SubscriptionId, storageInfo.ResourceGroup, input.QueueName)
                   {
                       Properties = new Dictionary<string, string>()
                       {
                           [AzureResourceInfoQueueDetailsProxy.StorageAccountName] = storageInfo.StorageAccountName,
                           [AzureResourceInfoQueueDetailsProxy.LocationName] = input.AzureLocation.ToString(),
                       },
                   };

                   return new QueueProviderCreateResult()
                   {
                       AzureResourceInfo = azureResourceInfo,
                       Status = OperationState.Succeeded,
                   };
               });
        }

        /// <inheritdoc/>
        public async Task<QueueProviderGetDetailsResult> GetDetailsAsync(
            QueueProviderGetDetailsInput input,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
               $"{LogBase}_get_details",
               async (childLogger) =>
               {
                   var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(input.AzureLocation, logger);
                   var azureResourceInfo = new AzureResourceInfo(storageInfo.SubscriptionId, storageInfo.ResourceGroup, input.Name)
                   {
                       Properties = new Dictionary<string, string>()
                       {
                           [AzureResourceInfoQueueDetailsProxy.StorageAccountName] = storageInfo.StorageAccountName,
                           [AzureResourceInfoQueueDetailsProxy.LocationName] = input.AzureLocation.ToString(),
                       },
                   };

                   return new QueueProviderGetDetailsResult()
                   {
                       AzureResourceInfo = azureResourceInfo,
                   };
               });
        }

        /// <inheritdoc/>
        public async Task<QueueProviderPushResult> PushMessageAsync(
            AzureResourceInfo azureResourceInfo,
            QueueMessage queueMessage,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
               $"{LogBase}_push_message",
               async (childLogger) =>
               {
                   var queueDetails = new AzureResourceInfoQueueDetailsProxy(azureResourceInfo);
                   var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(queueDetails.Location, logger);
                   var queue = GetQueueClient(storageInfo, azureResourceInfo.Name, logger);
                   var message = new CloudQueueMessage(JsonConvert.SerializeObject(queueMessage));

                   // Push the message to queue.
                   queue.AddMessage(message);

                   return new QueueProviderPushResult()
                   {
                       Status = OperationState.Succeeded,
                   };
               },
               (ex, childLogger) =>
                {
                    var result = new QueueProviderPushResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    childLogger.FluentAddValue(nameof(result.Status), result.Status.ToString())
                                       .FluentAddValue(nameof(result.ErrorReason), result.ErrorReason);
                    return Task.FromResult(result);
                },
               swallowException: true);
        }

        /// <inheritdoc/>
        public async Task ClearQueueAsync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
               $"{LogBase}_clear",
               async (childLogger) =>
               {
                   var queueDetails = new AzureResourceInfoQueueDetailsProxy(azureResourceInfo);
                   var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(queueDetails.Location, logger);
                   var queue = GetQueueClient(storageInfo, azureResourceInfo.Name, logger);
                   await queue.ClearAsync();
               });
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(
            QueueProviderDeleteInput input,
            IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
               $"{LogBase}_delete",
               async (childLogger) =>
               {
                   var queueDetails = new AzureResourceInfoQueueDetailsProxy(input.AzureResourceInfo);
                   var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(queueDetails.Location, logger);
                   var queue = GetQueueClient(storageInfo, input.AzureResourceInfo.Name, logger);
                   await queue.DeleteIfExistsAsync();
               });
        }

        /// <inheritdoc/>
        public async Task<object> ExistsAync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
              $"{LogBase}_exists",
              async (childLogger) =>
              {
                  var queueDetails = new AzureResourceInfoQueueDetailsProxy(azureResourceInfo);
                  var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(queueDetails.Location, logger);
                  var queue = GetQueueClient(storageInfo, azureResourceInfo.Name, logger);
                  var queueExists = await queue.ExistsAsync();
                  return queueExists ? new object() : default;
              });
        }

        /// <inheritdoc/>
        public async Task<QueueConnectionInfo> GetQueueConnectionInfoAsync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
              $"{LogBase}_get_connection_info",
              async (childLogger) =>
              {
                  var queueDetails = new AzureResourceInfoQueueDetailsProxy(azureResourceInfo);
                  var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(queueDetails.Location, logger);
                  var queue = GetQueueClient(storageInfo, azureResourceInfo.Name, logger);

                  // Get queue sas url
                  return await GetQueueConnectionInfoAsync(queue, logger);
              });
        }

        /// <inheritdoc/>
        async Task IQueueProviderDepricatedV0.DeleteAsync(
            AzureLocation location,
            string queueName,
            IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
              $"{LogBase}_depricated_delete",
              async (childLogger) =>
              {
                  var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(location, logger);
                  var queue = GetQueueClient(storageInfo, queueName, logger);
                  await queue.DeleteIfExistsAsync();
              });
        }

        /// <inheritdoc/>
        async Task<object> IQueueProviderDepricatedV0.ExistsAync(
            AzureLocation location,
            string queueName,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
              $"{LogBase}_depricated_exists",
              async (childLogger) =>
              {
                  var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(location, logger);
                  var queue = GetQueueClient(storageInfo, queueName, logger);
                  var queueExists = await queue.ExistsAsync();
                  return queueExists ? new object() : default;
              });
        }

        /// <inheritdoc/>
        async Task IQueueProviderDepricatedV0.PushMessageAsync(
            AzureLocation location,
            string queueName,
            QueueMessage queueMessage,
            IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
              $"{LogBase}_depricated_push_message",
              async (childLogger) =>
              {
                  var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(location, logger);
                  var queue = GetQueueClient(storageInfo, queueName, logger);
                  var message = new CloudQueueMessage(JsonConvert.SerializeObject(queueMessage));

                  // Push the message to queue.
                  queue.AddMessage(message);
              });
        }

        private CloudQueue GetQueueClient(ComputeQueueStorageInfo storageInfo, string queueName, IDiagnosticsLogger logger)
        {
            var storageCredentials = new StorageCredentials(storageInfo.StorageAccountName, storageInfo.StorageAccountKey);
            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            var queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);

            var queue = queueClient.GetQueueReference(queueName);
            return queue;
        }

        private Task<QueueConnectionInfo> GetQueueConnectionInfoAsync(
            CloudQueue cloudQueue,
            IDiagnosticsLogger logger)
        {
            try
            {
                var sasToken = cloudQueue.GetSharedAccessSignature(new SharedAccessQueuePolicy()
                {
                    Permissions = SharedAccessQueuePermissions.ProcessMessages,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddDays(SASTokenValidityInDays),
                });

                var connectionInfo = new QueueConnectionInfo()
                {
                    SasToken = sasToken,
                    Url = cloudQueue.ServiceClient.BaseUri.ToString(),
                    Name = cloudQueue.Name,
                };

                return Task.FromResult(connectionInfo);
            }
            catch (Exception e)
            {
                logger.LogException($"{LogBase}_virtual_machine_queue_connection_info_error", e);
                throw;
            }
        }
    }
}