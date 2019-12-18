// <copyright file="WindowsVirtualMachineManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Create, update and deletes Azure virtual machines.
    /// </summary>
    public class WindowsVirtualMachineManager : IDeploymentManager
    {
        private const string Key = "value";
        private const string NicNameKey = "nicName";
        private const string NsgNameKey = "nsgName";
        private const string VnetNameKey = "vnetName";
        private const string DiskNameKey = "diskId";
        private const string QueueNameKey = "queueName";
        private const string VmNameKey = "vmName";

        /// <summary>
        /// Name of the shim script which initializes a windows vm.
        /// The source of the shim script lives in the vsclk-cluster repository.
        /// When updated it release scripts for vsclk-cluster should be run on dev/ppe/prod to update the script on storage.
        /// Ideally no changes should be made to the shim script. Any init time additions should go to
        /// WindowsInit.ps1 in the Cascade repo.
        /// </summary>
        private const string WindowsInitShimScript = "WindowsInitShim.ps1";

        private static readonly string VmTemplateJson = GetVmTemplate();
        private readonly IAzureClientFactory clientFactory;
        private readonly IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsVirtualMachineManager"/> class.
        /// </summary>
        /// <param name="clientFactory">Builds Azure clients.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Control plane azure accessor.</param>
        public WindowsVirtualMachineManager(IAzureClientFactory clientFactory,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            Requires.NotNull(clientFactory, nameof(clientFactory));
            Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));

            this.clientFactory = clientFactory;
            this.controlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
        }

        /// <inheritdoc/>
        public bool Accepts(ComputeOS computeOS)
        {
            return computeOS == ComputeOS.Windows;
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
        {
            Requires.NotNull(input.AzureResourceGroup, nameof(input.AzureResourceGroup));
            Requires.NotNull(input.AzureSkuName, nameof(input.AzureSkuName));
            Requires.NotNull(input.AzureVirtualMachineImage, nameof(input.AzureVirtualMachineImage));
            Requires.NotNull(input.VMToken, nameof(input.VMToken));

            // create new VM resource name
            var virtualMachineName = GetVmName();

            var resourceTags = input.ResourceTags;
            resourceTags.Add(ResourceTagName.ResourceName, virtualMachineName);

            var deploymentName = $"Create-WindowsVm-{virtualMachineName}";
            try
            {
                var azure = await clientFactory.GetAzureClientAsync(input.AzureSubscription);
                await azure.CreateResourceGroupIfNotExistsAsync(input.AzureResourceGroup, input.AzureVmLocation.ToString());

                // Create input queue
                var queueName = GetQueueName(virtualMachineName);
                var queue = await GetQueueClientAsync(input.AzureVmLocation, queueName, logger);
                var queueCreated = await queue.CreateIfNotExistsAsync();
                if (!queueCreated)
                {
                    throw new VirtualMachineException($"Failed to create queue for virtual machine {virtualMachineName}");
                }

                // Get queue sas url
                var queueResult = await GetVirtualMachineInputQueueConnectionInfoAsync(input.AzureVmLocation, virtualMachineName, 0, logger);
                if (queueResult.Item1 != OperationState.Succeeded)
                {
                    throw new VirtualMachineException($"Failed to get sas token for virtual machine input queue {queue.Uri}");
                }

                // Get the queue SAS token to pass into the custom script.
                var queueConnectionInfo = queueResult.Item2;

                // Get information about the storage account to pass into the custom script.
                var storageInfo = await controlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeVmAgentImagesAsync(input.AzureVmLocation);
                var storageAccountName = storageInfo.Item1;
                var storageAccountAccessKey = storageInfo.Item2;
                var vmInitScriptFileUri = $"https://{storageAccountName}.blob.core.windows.net/windows-init-shim/{WindowsInitShimScript}";
                var userName = "vsonline";

                // Required parameters forwarded to the VM agent init script.
                // Be very careful removing parameters from this list because it can break the VM agent init script.
                var initScriptParametersBlob = new Dictionary<string, object>()
                {
                    { "inputQueueName", queueConnectionInfo.Name },
                    { "inputQueueUrl", queueConnectionInfo.Url },
                    { "inputQueueSasToken", queueConnectionInfo.SasToken },
                    { "vmToken", input.VMToken },
                    { "resourceId", input.ResourceId },
                    { "serviceHostName", input.FrontDnsHostName },
                    { "visualStudioInstallationDir", @"C:\VisualStudio" },
                    { "userName", userName },
                };

                // b64 encode the parameters json blob, this gets forwarded onto the VM agent script through the custom script extension.
                var parametersBlob = JsonConvert.SerializeObject(initScriptParametersBlob);
                var encodedBytes = Encoding.UTF8.GetBytes(parametersBlob);
                var b64ParametersBlob = Convert.ToBase64String(encodedBytes);

                var parameters = new Dictionary<string, Dictionary<string, object>>()
                {
                    { "location", new Dictionary<string, object>() { { Key, input.AzureVmLocation.ToString() } } },
                    { "imageReferenceId", new Dictionary<string, object>() { { Key, input.AzureVirtualMachineImage} } },
                    { "virtualMachineRG", new Dictionary<string, object>() { { Key, input.AzureResourceGroup } } },
                    { "virtualMachineName", new Dictionary<string, object>() { { Key, virtualMachineName} } },
                    { "virtualMachineSize", new Dictionary<string, object>() { { Key, input.AzureSkuName } } },
                    { "osDiskName", new Dictionary<string, object>() { { Key, GetOsDiskName(virtualMachineName) } } },
                    { "networkSecurityGroupName", new Dictionary<string, object>() { { Key, GetNetworkSecurityGroupName(virtualMachineName) } } },
                    { "networkInterfaceName", new Dictionary<string, object>() { { Key, GetNetworkInterfaceName(virtualMachineName) } } },
                    { "virtualNetworkName", new Dictionary<string, object>() { { Key, GetVirtualNetworkName(virtualMachineName) } } },
                    { "adminUserName", new Dictionary<string, object>() { { Key, userName } } },
                    { "adminPassword", new Dictionary<string, object>() { { Key, Guid.NewGuid() } } },
                    { "resourceTags", new Dictionary<string, object>() { { Key, resourceTags } } },
                    { "vmInitScriptFileUri", new Dictionary<string, object>() { { Key, vmInitScriptFileUri } } },
                    { "vmAgentBlobUrl", new Dictionary<string, object>() { { Key, input.VmAgentBlobUrl } } },
                    { "vmInitScriptStorageAccountName", new Dictionary<string, object>() { { Key, storageAccountName } } },
                    { "vmInitScriptStorageAccountKey", new Dictionary<string, object>() { { Key, storageAccountAccessKey } } },
                    { "vmInitScriptBase64ParametersBlob", new Dictionary<string, object>() { { Key, b64ParametersBlob } } },
                };

                // Create virtual machine
                var result = await azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(input.AzureResourceGroup)
                    .WithTemplate(VmTemplateJson)
                    .WithParameters(JsonConvert.SerializeObject(parameters))
                    .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                    .BeginCreateAsync();

                var azureResourceInfo = new AzureResourceInfo(input.AzureSubscription, input.AzureResourceGroup, virtualMachineName);
                return (OperationState.InProgress, new NextStageInput(result.Name, azureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("windows_virtual_machine_manager_begin_create_error", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> CheckCreateComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            try
            {
                var azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                var deployment = await azure.Deployments.GetByResourceGroupAsync(input.AzureResourceInfo.ResourceGroup, input.TrackingId);
                OperationState operationState = ParseResult(deployment.ProvisioningState);
                if (operationState == OperationState.Failed)
                {
                    var errorDetails = await DeploymentUtils.ExtractDeploymentErrors(deployment);
                    throw new VirtualMachineException(errorDetails);
                }

                return (operationState, new NextStageInput(input.TrackingId, input.AzureResourceInfo));
            }
            catch (VirtualMachineException vmException)
            {
                logger.LogException("windows_virtual_machine_manager_check_create_compute_error", vmException);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogException("windows_virtual_machine_manager_check_create_compute_error", ex);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1));
                }

                throw;
            }
        }

        /// <inheritdoc/>
        /// TODO: This will be completed for https://dev.azure.com/devdiv/DevDiv/_boards/board/t/BARS%20Crew/Stories/?workitem=968262
        public async Task<(OperationState, int)> StartComputeAsync(VirtualMachineProviderStartComputeInput input, int retryAttemptCount, IDiagnosticsLogger logger)
        {
            await Task.Delay(0); // to silence CS1998 until actual code is implemented
            return (OperationState.Failed, 0);
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> BeginDeleteComputeAsync(VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            try
            {
                var vmName = input.AzureResourceInfo.Name;
                var azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                var resourceGroup = input.AzureResourceInfo.ResourceGroup;
                var vmDeletionState = OperationState.Succeeded;
                var vm = await azure.VirtualMachines.GetByResourceGroupAsync(resourceGroup, vmName);
                if (vm != null)
                {
                    var computeClient = await clientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);
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
                resourcesToBeDeleted.Add(QueueNameKey, (GetQueueName(vmName), OperationState.NotStarted));

                return (OperationState.InProgress, new NextStageInput(
                    CreateVmDeletionTrackingId(input.AzureVmLocation, resourcesToBeDeleted),
                    input.AzureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("windows_virtual_machine_manager_begin_delete_error", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> CheckDeleteComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            try
            {
                var (computeVmLocation, resourcesToBeDeleted) = JsonConvert
                .DeserializeObject<(AzureLocation, Dictionary<string, VmResourceState>)>(input.TrackingId);
                string resourceGroup = input.AzureResourceInfo.ResourceGroup;
                var azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                var networkClient = await clientFactory.GetNetworkManagementClient(input.AzureResourceInfo.SubscriptionId);
                var computeClient = await clientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);

                if (resourcesToBeDeleted[VmNameKey].State != OperationState.Succeeded)
                {
                    // Check if virtual machine deletion is complete
                    var vm = await azure.VirtualMachines.GetByResourceGroupAsync(resourceGroup, resourcesToBeDeleted[VmNameKey].Name);
                    if (vm != null)
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
                    QueueNameKey));

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

                logger.LogError($"windows_virtual_machine_manager_check_delete_compute_error : \n {s}");
                var nextStageInput = new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, nextStageInput);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, QueueConnectionInfo, int)> GetVirtualMachineInputQueueConnectionInfoAsync(AzureLocation location, string virtualMachineName, int retryAttemptCount, IDiagnosticsLogger logger)
        {
            try
            {
                var queueClient = await GetQueueClientAsync(location, GetQueueName(virtualMachineName), logger);
                var sas = queueClient.GetSharedAccessSignature(new SharedAccessQueuePolicy()
                {
                    Permissions = SharedAccessQueuePermissions.ProcessMessages,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddDays(365),
                });

                return (OperationState.Succeeded, new QueueConnectionInfo(queueClient.Name, queueClient.ServiceClient.BaseUri.ToString(), sas), 0);
            }
            catch (Exception ex)
            {
                logger.LogException("windows_virtual_machine_manager_get_input_queue_url_error", ex);
                if (retryAttemptCount < 5)
                {
                    return (OperationState.InProgress, default, retryAttemptCount + 1);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, int)> ShutdownComputeAsync(VirtualMachineProviderShutdownInput input, int retryAttempt, IDiagnosticsLogger logger)
        {
            try
            {
                var vmAgentQueueMessage = new QueueMessage
                {
                    Command = "ShutdownEnvironment",
                    Id = input.EnvironmentId,
                };

                var message = new CloudQueueMessage(JsonConvert.SerializeObject(vmAgentQueueMessage));
                var queue = await GetQueueClientAsync(input.AzureVmLocation, GetQueueName(input.AzureResourceInfo.Name), logger);

                // Push the message to queue.
                queue.AddMessage(message);

                return (OperationState.Succeeded, 0);
            }
            catch (Exception ex)
            {
                logger.LogException("windows_virtual_machine_manager_shutdown_compute_error", ex);

                if (retryAttempt < 5)
                {
                    return (OperationState.InProgress, retryAttempt + 1);
                }

                throw;
            }
        }

        private static OperationState GetFinalState(Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            if (resourcesToBeDeleted.Any(r => r.Value.State == OperationState.InProgress || r.Value.State == OperationState.NotStarted))
            {
                return OperationState.InProgress;
            }

            return OperationState.Succeeded;
        }

        private static Task CheckResourceStatus<TResult>(
           Func<string, Task> deleteResourceFunc,
           Func<string, Task<TResult>> checkResourceFunc,
           Dictionary<string, VmResourceState> resourcesToBeDeleted,
           string resourceNameKey)
        {
            string resourceName = resourcesToBeDeleted[resourceNameKey].Name;
            OperationState resourceState = resourcesToBeDeleted[resourceNameKey].State;
            if (resourceState == OperationState.NotStarted)
            {
                Task beginDeleteTask = deleteResourceFunc(resourceName);
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
                Task<TResult> checkStatusTask = checkResourceFunc(resourceName);
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

        private static OperationState ParseResult(string provisioningState)
        {
            if (provisioningState.Equals(OperationState.Succeeded.ToString(), StringComparison.OrdinalIgnoreCase)
           || provisioningState.Equals(OperationState.Failed.ToString(), StringComparison.OrdinalIgnoreCase)
           || provisioningState.Equals(OperationState.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return provisioningState.ToEnum<OperationState>();
            }

            return OperationState.InProgress;
        }

        private static string GetVmName()
        {
            // Windows computer names cannot exceed 15 chars or contain any of these chars: ~ ! @ # $ % ^ & * ( ) = + _ [ ] { } \ | ; : . ' " , < > / ?
            // This code is based on https://devdiv.visualstudio.com/DevDiv/_git/Cascade/?path=%2Fsrc%2FServices%2FCascade.Services.Common.Repositories%2FProviders%2FWorkspaceIdProvider.cs&version=GBmaster
            const string Valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var length = 15;
            var result = new char[length];
            var buffer = new byte[sizeof(uint)];
            using (var provider = new RNGCryptoServiceProvider())
            {
                while (length-- > 0)
                {
                    provider.GetBytes(buffer);
                    var value = BitConverter.ToUInt32(buffer, 0);
                    result[length] = Valid[(int)(value % (uint)Valid.Length)];
                }
            }

            return new string(result);
        }

        private static string GetVmTemplate()
        {
            return GetEmbeddedResource("template_vm.json");
        }

        private static string GetEmbeddedResource(string resourceName)
        {
            string namespaceString = typeof(WindowsVirtualMachineManager).Namespace;
            var fullResourceName = $"{namespaceString}.Templates.Windows.{resourceName}";
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }

        private static string CreateVmDeletionTrackingId(AzureLocation computeVmLocation, Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            return JsonConvert.SerializeObject((computeVmLocation, resourcesToBeDeleted));
        }

        private static string GetQueueName(string vmName)
        {
            // Queue names must be all lowercase, but the VM name and other resources use mixed case
            // to reduce chances of name collisions. (See GetVmName for details on why.)
            return $"{vmName.ToLower()}-input-queue";
        }

        private static string GetVirtualNetworkName(string vmName)
        {
            return $"{vmName}-vnet";
        }

        private static string GetNetworkInterfaceName(string vmName)
        {
            return $"{vmName}-nic";
        }

        private static string GetNetworkSecurityGroupName(string vmName)
        {
            return $"{vmName}-nsg";
        }

        private static string GetOsDiskName(string vmName)
        {
            return $"{vmName}-disk";
        }

        private async Task<CloudQueue> GetQueueClientAsync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var (accountName, accountKey) = await controlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(location, logger);
            var storageCredentials = new StorageCredentials(accountName, accountKey);
            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            var queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
            var queue = queueClient.GetQueueReference(queueName);
            return queue;
        }

        private async Task DeleteQueueAsync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueClientAsync(location, queueName, logger);
            await queue.DeleteIfExistsAsync();
        }

        private async Task<object> QueueExistsAync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueClientAsync(location, queueName, logger);
            var queueExists = await queue.ExistsAsync();
            return queueExists ? new object() : default;
        }
    }
}