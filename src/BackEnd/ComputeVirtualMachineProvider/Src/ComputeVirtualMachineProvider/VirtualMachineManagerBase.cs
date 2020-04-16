// <copyright file="VirtualMachineManagerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Abstract base class for virtual machine manager.
    /// </summary>
    public abstract class VirtualMachineManagerBase : IDeploymentManager
    {
        /// <summary>
        /// Input queue name token.
        /// </summary>
        public const string InputQueueNameKey = "inputQueueName";

        /// <summary>
        /// Output queue name token.
        /// </summary>
        public const string OutputQueueNameKey = "outputQueueName";

        /// <summary>
        /// Value token.
        /// </summary>
        public const string Key = "value";

        /// <summary>
        /// Nic name token.
        /// </summary>
        public const string NicNameKey = "nicName";

        /// <summary>
        /// Nsg name token.
        /// </summary>
        public const string NsgNameKey = "nsgName";

        /// <summary>
        /// Vnet name token.
        /// </summary>
        public const string VnetNameKey = "vnetName";

        /// <summary>
        /// DiskId name token.
        /// </summary>
        public const string DiskNameKey = "diskId";

        /// <summary>
        /// Queue name token.
        /// </summary>
        public const string QueueNameKey = "queueName";

        /// <summary>
        /// vmName token.
        /// </summary>
        public const string VmNameKey = "vmName";

        private const string LogBase = "virtual_machine_manager_base";

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineManagerBase"/> class.
        /// </summary>
        /// <param name="clientFactory">Azure client factory.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Control plane azure resource accessor object.</param>
        public VirtualMachineManagerBase(
            IAzureClientFactory clientFactory,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            this.ControlPlaneAzureResourceAccessor = Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
            this.ClientFactory = Requires.NotNull(clientFactory, nameof(clientFactory));
            VmTemplateJson = GetVmTemplate();
            SetUseOutputQueueFlag();
        }

        /// <summary>
        /// Gets a value indicating whether the output queue is enabled.
        /// </summary>
        public bool UseOutputQueue { get; private set; }

        /// <summary>
        /// Gets controlPlaneAzureResourceAccessor object.
        /// </summary>
        public IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; }

        /// <summary>
        /// Gets the azure client factory object.
        /// </summary>
        public IAzureClientFactory ClientFactory { get; }

        /// <summary>
        /// Gets the VM template.
        /// </summary>
        public string VmTemplateJson { get; }

        /// <summary>
        /// Gets name of the output queue.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the output queue.</returns>
        public static string GetOutputQueueName(string vmName) => $"{vmName.ToLowerInvariant()}-output-queue";

        /// <summary>
        /// Gets the name of the input qeue.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the input queue.</returns>
        public static string GetInputQueueName(string vmName) => $"{vmName.ToLowerInvariant()}-input-queue";

        /// <summary>
        /// Gets name of the Os Disk.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the Os disk.</returns>
        public static string GetOsDiskName(string vmName) => $"{vmName}-disk";

        /// <summary>
        /// Gets name of the virtual network.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the virtual network.</returns>
        public static string GetVirtualNetworkName(string vmName) => $"{vmName}-vnet";

        /// <summary>
        /// Gets name of the security group.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the security group.</returns>
        public static string GetNetworkSecurityGroupName(string vmName) => $"{vmName}-nsg";

        /// <summary>
        /// Gets name of the network interface.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the network interface.</returns>
        public static string GetNetworkInterfaceName(string vmName) => $"{vmName}-nic";

        /// <inheritdoc/>
        public abstract bool Accepts(ComputeOS computeOS);

        /// <inheritdoc/>
        public abstract Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger);

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, NextStageInput NextInput)> BeginDeleteComputeAsync(VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            try
            {
                var vmName = input.AzureResourceInfo.Name;
                var azure = await ClientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                var resourceGroup = input.AzureResourceInfo.ResourceGroup;
                var vm = await azure.VirtualMachines
                                  .GetByResourceGroupAsync(resourceGroup, vmName);

                var vmDeletionState = OperationState.Succeeded;
                if (vm != null)
                {
                    var computeClient = await ClientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);
                    await computeClient.VirtualMachines.BeginDeleteAsync(resourceGroup, vmName);
                    vmDeletionState = OperationState.InProgress;
                }

                // Save resource state for continuation token.
                var resourcesToBeDeleted = new Dictionary<string, VmResourceState>();
                resourcesToBeDeleted.Add(VmNameKey, (vmName, vmDeletionState));
                resourcesToBeDeleted.Add(NicNameKey, (GetNetworkInterfaceName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(NsgNameKey, (GetNetworkSecurityGroupName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(VnetNameKey, (GetVirtualNetworkName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(DiskNameKey, (GetOsDiskName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(InputQueueNameKey, (GetInputQueueName(vmName), OperationState.NotStarted));
                AddOutputQueue(vmName, resourcesToBeDeleted);

                return (OperationState.InProgress, new NextStageInput(
                    CreateVmDeletionTrackingId(input.AzureVmLocation, resourcesToBeDeleted),
                    input.AzureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_begin_delete_compute_error", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, NextStageInput NextInput)> CheckCreateComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            try
            {
                var azure = await ClientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                var deployment = await azure.Deployments.GetByResourceGroupAsync(input.AzureResourceInfo.ResourceGroup, input.TrackingId);

                OperationState operationState = DeploymentUtils.ParseProvisioningState(deployment.ProvisioningState);
                if (operationState == OperationState.Failed)
                {
                    var errorDetails = await DeploymentUtils.ExtractDeploymentErrors(deployment);
                    throw new VirtualMachineException(errorDetails);
                }

                return (operationState, new NextStageInput(input.TrackingId, input.AzureResourceInfo));
            }
            catch (VirtualMachineException vmException)
            {
                logger.LogException($"{LogBase}_check_create_compute_status_error", vmException);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_check_create_compute_status_error", ex);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1));
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, NextStageInput NextInput)> CheckDeleteComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            try
            {
                var (computeVmLocation, resourcesToBeDeleted) = JsonConvert
                .DeserializeObject<(AzureLocation, Dictionary<string, VmResourceState>)>(input.TrackingId);
                string resourceGroup = input.AzureResourceInfo.ResourceGroup;
                var azure = await ClientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                var networkClient = await ClientFactory.GetNetworkManagementClient(input.AzureResourceInfo.SubscriptionId);
                var computeClient = await ClientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);

                if (resourcesToBeDeleted[VmNameKey].State != OperationState.Succeeded)
                {
                    // Check if virtual machine deletion is complete
                    var linuxVM = await azure.VirtualMachines
                              .GetByResourceGroupAsync(resourceGroup, resourcesToBeDeleted[VmNameKey].Name);
                    if (linuxVM != null)
                    {
                        return (OperationState.InProgress, input);
                    }
                    else
                    {
                        resourcesToBeDeleted[VmNameKey] = (resourcesToBeDeleted[VmNameKey].Name, OperationState.Succeeded);
                    }
                }

                // Virtual machine is deleted, delete the remaining resources
                var taskList = new List<Task>();

                taskList.Add(
                CheckResourceStatus(
                    (resourceName) => DeleteQueueAsync(computeVmLocation, resourceName, logger),
                    (resourceName) => QueueExistsAync(computeVmLocation, resourceName, logger),
                    resourcesToBeDeleted,
                    InputQueueNameKey));

                DeleteOutputQueue(logger, computeVmLocation, resourcesToBeDeleted, taskList);

                taskList.Add(
                CheckResourceStatus(
                    (resourceName) => computeClient.Disks.BeginDeleteAsync(resourceGroup, resourceName),
                    (resourceName) => azure.Disks.GetByResourceGroupAsync(resourceGroup, resourceName),
                    resourcesToBeDeleted,
                    DiskNameKey));

                taskList.Add(
                    CheckResourceStatus(
                      (resourceName) => networkClient.NetworkInterfaces.BeginDeleteAsync(resourceGroup, resourceName),
                      (resourceName) => azure.NetworkInterfaces.GetByResourceGroupAsync(resourceGroup, resourceName),
                      resourcesToBeDeleted,
                      NicNameKey));

                if (resourcesToBeDeleted[NicNameKey].State == OperationState.Succeeded)
                {
                    taskList.Add(
                        CheckResourceStatus(
                        (resourceName) => networkClient.NetworkSecurityGroups.BeginDeleteAsync(resourceGroup, resourceName),
                        (resourceName) => azure.NetworkSecurityGroups.GetByResourceGroupAsync(resourceGroup, resourceName),
                        resourcesToBeDeleted,
                        NsgNameKey));

                    taskList.Add(
                        CheckResourceStatus(
                        (resourceName) => networkClient.VirtualNetworks.BeginDeleteAsync(resourceGroup, resourceName),
                        (resourceName) => azure.Networks.GetByResourceGroupAsync(resourceGroup, resourceName),
                        resourcesToBeDeleted,
                        VnetNameKey));
                }

                await Task.WhenAll(taskList);
                var nextStageInput = new NextStageInput(CreateVmDeletionTrackingId(computeVmLocation, resourcesToBeDeleted), input.AzureResourceInfo);
                var resultState = GetFinalState(resourcesToBeDeleted);
                return (resultState, nextStageInput);
            }
            catch (AggregateException ex)
            {
                var s = new StringBuilder();
                foreach (var e in ex.Flatten().InnerExceptions)
                {
                    s.AppendLine("Exception type: " + e.GetType().FullName);
                    s.AppendLine("Message       : " + e.Message);
                    s.AppendLine("Stacktrace:");
                    s.AppendLine(e.StackTrace);
                    s.AppendLine();
                }

                logger.LogErrorWithDetail($"{LogBase}__error", s.ToString());
                NextStageInput nextStageInput = new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, nextStageInput);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, int RetryAttempt)> ShutdownComputeAsync(VirtualMachineProviderShutdownInput input, int retryAttempt, IDiagnosticsLogger logger)
        {
            try
            {
                var vmAgentQueueMessage = new QueueMessage
                {
                    Command = "ShutdownEnvironment",
                    Id = input.EnvironmentId,
                };

                var message = new CloudQueueMessage(JsonConvert.SerializeObject(vmAgentQueueMessage));
                var queue = await GetQueueClientAsync(input.AzureVmLocation, GetInputQueueName(input.AzureResourceInfo.Name), logger);

                // Push the message to queue.
                queue.AddMessage(message);

                return (OperationState.Succeeded, 0);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_shutdown_compute_error", ex);

                if (retryAttempt < 5)
                {
                    return (OperationState.InProgress, retryAttempt + 1);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, int RetryAttempt)> StartComputeAsync(VirtualMachineProviderStartComputeInput input, int retryAttemptCount, IDiagnosticsLogger logger)
        {
            try
            {
                // create queue message
                var jobParameters = new Dictionary<string, string>();
                foreach (var kvp in input.VmInputParams)
                {
                    jobParameters.Add(kvp.Key, kvp.Value);
                }

                jobParameters.Add("storageAccountName", input.FileShareConnection.StorageAccountName);
                jobParameters.Add("storageAccountKey", input.FileShareConnection.StorageAccountKey);
                jobParameters.Add("storageShareName", input.FileShareConnection.StorageShareName);
                jobParameters.Add("storageFileName", input.FileShareConnection.StorageFileName);

                // Temporary: Add sku so the vm agent can limit memory on DS4_v3 VMs.
                jobParameters.Add("skuName", input.SkuName);

                var queueMessage = new QueueMessage
                {
                    Command = "StartEnvironment",
                    Parameters = jobParameters,
                };

                var message = new CloudQueueMessage(JsonConvert.SerializeObject(queueMessage));
                var queue = await GetQueueClientAsync(input.Location, GetInputQueueName(input.AzureResourceInfo.Name), logger);

                // post message to queue
                queue.AddMessage(message);

                return (OperationState.Succeeded, 0);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_start_compute_error", ex);

                if (retryAttemptCount < 5)
                {
                    return (OperationState.InProgress, retryAttemptCount + 1);
                }

                throw;
            }
        }

        /// <summary>
        /// Creates a queue.
        /// </summary>
        /// <param name="input">VirtualMachine create input parameters.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <param name="virtualMachineName">Virtual machine name.</param>
        /// <param name="queueName">Queue name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected async Task<QueueConnectionInfo> CreateQueue(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger, string virtualMachineName, string queueName)
        {
            var queue = await GetQueueClientAsync(input.AzureVmLocation, queueName, logger);
            var queueCreated = await queue.CreateIfNotExistsAsync();
            if (!queueCreated)
            {
                throw new VirtualMachineException($"Failed to create queue for virtual machine {virtualMachineName}");
            }

            // Get queue sas url
            var queueResult = await GetVirtualMachineQueueConnectionInfoAsync(queue, 0, logger);
            if (queueResult.Item1 != OperationState.Succeeded)
            {
                throw new VirtualMachineException($"Failed to get sas token for virtual machine input queue {queue.Uri}");
            }

            var queueConnectionInfo = queueResult.Item2;
            return queueConnectionInfo;
        }

        /// <summary>
        /// Gets VM template.
        /// </summary>
        /// <returns>VM template.</returns>
        protected abstract string GetVmTemplate();

        private static string CreateVmDeletionTrackingId(AzureLocation computeVmLocation, Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            return JsonConvert.SerializeObject((computeVmLocation, resourcesToBeDeleted));
        }

        private static OperationState GetFinalState(Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            if (resourcesToBeDeleted.Any(r => r.Value.State == OperationState.InProgress || r.Value.State == OperationState.NotStarted))
            {
                return OperationState.InProgress;
            }

            return OperationState.Succeeded;
        }

        /// <summary>
        /// Checks the status of the resource.
        /// </summary>
        /// <typeparam name="TResult">type param.</typeparam>
        /// <param name="deleteResourceFunc">delete resource delegate.</param>
        /// <param name="checkResourceFunc">check resource delegate.</param>
        /// <param name="resourcesToBeDeleted">list of resources to be deleted.</param>
        /// <param name="resourceNameKey">resource name key.</param>
        /// <returns>A task.</returns>
        private static Task CheckResourceStatus<TResult>(
           Func<string, Task> deleteResourceFunc,
           Func<string, Task<TResult>> checkResourceFunc,
           Dictionary<string, VmResourceState> resourcesToBeDeleted,
           string resourceNameKey)
        {
            var resourceName = resourcesToBeDeleted[resourceNameKey].Name;
            var resourceState = resourcesToBeDeleted[resourceNameKey].State;
            if (resourceState == OperationState.NotStarted)
            {
                var beginDeleteTask = deleteResourceFunc(resourceName);
                return beginDeleteTask.ContinueWith(
                        (task) =>
                        {
                            if (task.IsCompleted)
                            {
                                resourcesToBeDeleted[resourceNameKey] = (resourceName, OperationState.InProgress);
                            }
                            else if (task.IsFaulted || task.IsCanceled)
                            {
                                resourcesToBeDeleted[resourceNameKey] = (resourceName, OperationState.NotStarted);
                            }
                        });
            }
            else if (resourceState == OperationState.InProgress)
            {
                var checkStatusTask = checkResourceFunc(resourceName);
                return checkStatusTask.ContinueWith(
                        (task) =>
                        {
                            if (task.IsCompleted && task.Result == null)
                            {
                                resourcesToBeDeleted[resourceNameKey] = (resourceName, OperationState.Succeeded);
                            }
                        });
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Deletes output queue by adding it to the tasklist.
        /// </summary>
        /// <param name="logger">Diagnostics logger.</param>
        /// <param name="computeVmLocation">Azure location.</param>
        /// <param name="resourcesToBeDeleted">Dictionary of resources to be deleted.</param>
        /// <param name="taskList">List of task.</param>
        [Conditional("DEBUG")]
        private void DeleteOutputQueue(IDiagnosticsLogger logger, AzureLocation computeVmLocation, Dictionary<string, VmResourceState> resourcesToBeDeleted, List<Task> taskList)
        {
            taskList.Add(
            CheckResourceStatus(
               (resourceName) => DeleteQueueAsync(computeVmLocation, resourceName, logger),
               (resourceName) => QueueExistsAync(computeVmLocation, resourceName, logger),
               resourcesToBeDeleted,
               OutputQueueNameKey));
        }

        [Conditional("DEBUG")]
        private void SetUseOutputQueueFlag()
        {
            this.UseOutputQueue = true;
        }

        /// <summary>
        /// Marks output queue for deletion.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <param name="resourcesToBeDeleted">Dictionary of resources to be deleted.</param>
        [Conditional("DEBUG")]
        private void AddOutputQueue(string vmName, Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            resourcesToBeDeleted.Add(OutputQueueNameKey, (GetOutputQueueName(vmName), OperationState.NotStarted));
        }

        /// <summary>
        /// Gets a queue client.
        /// </summary>
        /// <param name="location">Azure location.</param>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task<CloudQueue> GetQueueClientAsync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var (accountName, accountKey) = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(location, logger);
            var storageCredentials = new StorageCredentials(accountName, accountKey);
            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            var queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
            var queue = queueClient.GetQueueReference(queueName);
            return queue;
        }

        /// <summary>
        /// Deletes a queue.
        /// </summary>
        /// <param name="location">Azure location.</param>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        private async Task DeleteQueueAsync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueClientAsync(location, queueName, logger);
            await queue.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Check for existance of a queue.
        /// </summary>
        /// <param name="location">Azure location.</param>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task<object> QueueExistsAync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueClientAsync(location, queueName, logger);
            var queueExists = await queue.ExistsAsync();
            return queueExists ? new object() : default;
        }

        private Task<(OperationState, QueueConnectionInfo, int)> GetVirtualMachineQueueConnectionInfoAsync(CloudQueue cloudQueue, int retryAttempCount, IDiagnosticsLogger logger)
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
