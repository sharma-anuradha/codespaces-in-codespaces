// <copyright file="VirtualMachineQueueProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
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
using QueueMessage = Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models.QueueMessage;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider
{
    /// <summary>
    /// Manages virtual machine queue.
    /// </summary>
    public class VirtualMachineQueueProvider : IQueueProvider
    {
        private const string LogBase = "queue_provider";

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
        public async Task<QueueConnectionInfo> CreateQueue(
            QueueProviderCreateInput input,
            IDiagnosticsLogger logger)
        {
            var queue = await GetQueueClientAsync(input.AzureLocation, input.QueueName, logger);
            var queueCreated = await queue.CreateIfNotExistsAsync();
            if (!queueCreated)
            {
                throw new DeploymentException($"Failed to create queue for virtual machine {input.QueueName}");
            }

            // Get queue sas url
            var queueResult = await GetQueueConnectionInfoAsync(queue, 0, logger);
            if (queueResult.Item1 != OperationState.Succeeded)
            {
                throw new DeploymentException($"Failed to get sas token for virtual machine input queue {queue.Uri}");
            }

            var queueConnectionInfo = queueResult.Item2;
            return queueConnectionInfo;
        }

        /// <inheritdoc/>
        public async Task PushMessageAsync(
            AzureLocation location,
            string queueName,
            QueueMessage queueMessage,
            IDiagnosticsLogger logger)
        {
            var queue = await GetQueueClientAsync(
                    location,
                    queueName,
                    logger);

            var message = new CloudQueueMessage(JsonConvert.SerializeObject(queueMessage));

            // Push the message to queue.
            queue.AddMessage(message);
        }

        /// <inheritdoc/>
        public async Task DeleteQueueAsync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueClientAsync(location, queueName, logger);
            await queue.DeleteIfExistsAsync();
        }

        /// <inheritdoc/>
        public async Task<object> QueueExistsAync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueClientAsync(location, queueName, logger);
            var queueExists = await queue.ExistsAsync();
            return queueExists ? new object() : default;
        }

        private async Task<CloudQueue> GetQueueClientAsync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var (accountName, accountKey) = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(location, logger);
            var storageCredentials = new StorageCredentials(accountName, accountKey);
            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            var queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
            var queue = queueClient.GetQueueReference(queueName);
            return queue;
        }

        private Task<(OperationState, QueueConnectionInfo, int)> GetQueueConnectionInfoAsync(
            CloudQueue cloudQueue,
            int retryAttempCount,
            IDiagnosticsLogger logger)
        {
            try
            {
                var sas = cloudQueue.GetSharedAccessSignature(new SharedAccessQueuePolicy()
                {
                    Permissions = SharedAccessQueuePermissions.ProcessMessages,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddDays(365),
                });

                return Task.FromResult((OperationState.Succeeded, new QueueConnectionInfo(cloudQueue.Name, cloudQueue.ServiceClient.BaseUri.ToString(), sas), 0));
            }
            catch (Exception e)
            {
                logger.LogException($"{LogBase}_virtual_machine_queue_connection_info_error", e);
                if (retryAttempCount < 5)
                {
                    return Task.FromResult<(OperationState, QueueConnectionInfo, int)>((OperationState.InProgress, default, retryAttempCount + 1));
                }

                throw;
            }
        }
    }
}