// <copyright file="WindowsVirtualMachineManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
        public bool Accepts(AzureResourceInfo info)
        {
            // For delete, the resource name may never be set if the resource failed to create in Azure. In this case,
            // assume the windows manager is NOT able to handle the delete.
            return info.Name?.EndsWith("-win") ?? false;
        }

        /// <inheritdoc/>
        public bool Accepts(VirtualMachineProviderCreateInput input)
        {
            //TODO: This is a hack; we don't have the proper config info at this level. JohnRi and AnVan to work out how to get this.
            //What we really want is just a single Accepts() method that can find out the ComputeOS type
            return input.AzureSkuName.Contains("-Windows-");
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
        {
            // create new VM resource name
            var resourceName = $"{Guid.NewGuid().ToString()}-win";

            var resourceTags = input.ResourceTags;

            var parameters = new Dictionary<string, Dictionary<string, object>>()
            {
                { "location", new Dictionary<string, object>() { { Key, input.AzureVmLocation.ToString() } } },
                { "imageReferenceId", new Dictionary<string, object>() { { Key, input.AzureVirtualMachineImage} } },
                { "virtualMachineRG", new Dictionary<string, object>() { { Key, input.AzureResourceGroup } } },
                { "virtualMachineName", new Dictionary<string, object>() { { Key, resourceName} } },
                { "virtualMachineSize", new Dictionary<string, object>() { { Key, input.AzureSkuName } } },
                { "osDiskName", new Dictionary<string, object>() { { Key, GetOsDiskName(resourceName) } } },
                { "networkSecurityGroupName", new Dictionary<string, object>() { { Key, GetNetworkSecurityGroupName(resourceName) } } },
                { "networkInterfaceName", new Dictionary<string, object>() { { Key, GetNetworkInterfaceName(resourceName) } } },
                { "virtualNetworkName", new Dictionary<string, object>() { { Key, GetVirtualNetworkName(resourceName) } } },
                { "adminUserName", new Dictionary<string, object>() { { Key, "cloudenv" } } },
                { "adminPassword", new Dictionary<string, object>() { { Key, Guid.NewGuid() } } },
                { "resourceTags", new Dictionary<string, object>() { { Key, resourceTags } } },
            };

            var deploymentName = $"Create-Vm-{resourceName}";
            try
            {
                var azure = await clientFactory.GetAzureClientAsync(input.AzureSubscription);
                await azure.CreateResourceGroupIfNotExistsAsync(input.AzureResourceGroup, input.AzureVmLocation.ToString());
                var result = await azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(input.AzureResourceGroup)
                    .WithTemplate(VmTemplateJson)
                    .WithParameters(JsonConvert.SerializeObject(parameters))
                    .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                    .BeginCreateAsync();

                var azureResourceInfo = new AzureResourceInfo(input.AzureSubscription, input.AzureResourceGroup, resourceName);
                return (OperationState.InProgress, new NextStageInput(result.Name, azureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("windows_virtual_machine_manager_begin_create_error", ex);
                return (OperationState.Failed, default);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> CheckCreateComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            try
            {
                IAzure azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                IDeployment deployment = await azure.Deployments.GetByResourceGroupAsync(input.AzureResourceInfo.ResourceGroup, input.TrackingId);
                return (ParseResult(deployment.ProvisioningState), new NextStageInput(input.TrackingId, input.AzureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("windows_virtual_machine_manager_check_create_compute_error", ex);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1));
                }

                return (OperationState.Failed, default);
            }
        }

        /// <inheritdoc/>
        /// TODO: This will be completed for https://dev.azure.com/devdiv/DevDiv/_boards/board/t/BARS%20Crew/Stories/?workitem=968262
        public async Task<(OperationState, NextStageInput)> BeginStartComputeAsync(VirtualMachineProviderStartComputeInput input, IDiagnosticsLogger logger)
        {
            await Task.Delay(0); // to silence CS1998 until actual code is implemented
            return (OperationState.Failed, default);
        }

        /// <inheritdoc/>
        /// TODO: This will be completed for https://dev.azure.com/devdiv/DevDiv/_boards/board/t/BARS%20Crew/Stories/?workitem=968262
        public async Task<(OperationState, NextStageInput)> CheckStartComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            await Task.Delay(0); // to silence CS1998 until actual code is implemented
            return (OperationState.Failed, default);
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> BeginDeleteComputeAsync(VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            try
            {
                string vmName = input.AzureResourceInfo.Name;
                IAzure azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                string resourceGroup = input.AzureResourceInfo.ResourceGroup;
                IVirtualMachine vm = await azure.VirtualMachines.GetByResourceGroupAsync(resourceGroup, vmName);
                var resourcesToBeDeleted = new Dictionary<string, VmResourceState>();

                OperationState vmDeletionState = OperationState.Succeeded;
                if (vm != null)
                {
                    IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);
                    await computeClient.VirtualMachines.BeginDeleteAsync(resourceGroup, vmName);
                    vmDeletionState = OperationState.InProgress;
                }

                // Save resource state for continuation token.
                resourcesToBeDeleted.Add(VmNameKey, (vmName, vmDeletionState));
                resourcesToBeDeleted.Add(NicNameKey, (GetNetworkInterfaceName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(NsgNameKey, (GetNetworkSecurityGroupName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(VnetNameKey, (GetVirtualNetworkName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(DiskNameKey, (GetOsDiskName(vmName), OperationState.NotStarted));

                return (OperationState.InProgress, new NextStageInput(JsonConvert.SerializeObject(resourcesToBeDeleted), input.AzureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("windows_virtual_machine_manager_begin_delete_error", ex);
                return (OperationState.Failed, default);
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
                IAzure azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                INetworkManagementClient networkClient = await clientFactory.GetNetworkManagementClient(input.AzureResourceInfo.SubscriptionId);
                IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);

                if (resourcesToBeDeleted[VmNameKey].State != OperationState.Succeeded)
                {
                    // Check if virtual machine deletion is complete
                    IVirtualMachine vm = await azure.VirtualMachines.GetByResourceGroupAsync(resourceGroup, resourcesToBeDeleted[VmNameKey].Name);
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
                var nextStageInput = new NextStageInput(JsonConvert.SerializeObject(resourcesToBeDeleted), input.AzureResourceInfo);
                var resultState = GetFinalState(resourcesToBeDeleted);

                // TODO:: remove it when Resource Group creation logic is fixed.
                if (resultState == OperationState.Succeeded)
                {
                    await azure.DeleteResourceGroupAsync(resourceGroup);
                }

                return (resultState, nextStageInput);
            }
            catch (AggregateException ex)
            {
                StringBuilder s = new StringBuilder();
                foreach (var e in ex.Flatten().InnerExceptions)
                {
                    s.AppendLine("Exception type: " + e.GetType().FullName);
                    s.AppendLine("Message       : " + e.Message);
                    s.AppendLine("Stacktrace:");
                    s.AppendLine(e.StackTrace);
                    s.AppendLine();
                }

                logger.LogError($"windows_virtual_machine_manager_check_delete_compute_error : \n {s}");
                NextStageInput nextStageInput = new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, nextStageInput);
                }

                return (OperationState.Failed, nextStageInput);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, QueueConnectionInfo, int)> GetVirtualMachineInputQueueConnectionInfoAsync(AzureLocation location, string virtualMachineName, int retryAttempCount, IDiagnosticsLogger logger)
        {
            try
            {
                var queueClient = await GetQueueClientAsync(location, GetQueueName(virtualMachineName), logger);
                var sas = queueClient.GetSharedAccessSignature(new SharedAccessQueuePolicy()
                {
                    Permissions = SharedAccessQueuePermissions.Read,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddDays(10),
                });

                return (OperationState.Succeeded, new QueueConnectionInfo(queueClient.Uri.ToString(), sas), 0);
            }
            catch (Exception ex)
            {
                logger.LogException("windows_virtual_machine_manager_get_input_queue_url_error", ex);
                if (retryAttempCount < 5)
                {
                    return (OperationState.InProgress, default, retryAttempCount + 1);
                }

                return (OperationState.Failed, default, 0);
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

        private static string GetQueueName(string resourceName)
        {
            return $"{resourceName}-input-queue";
        }

        private static string GetVirtualNetworkName(string resourceName)
        {
            return $"{resourceName}-vnet";
        }

        private static string GetNetworkInterfaceName(string resourceName)
        {
            return $"{resourceName}-nic";
        }

        private static string GetNetworkSecurityGroupName(string resourceName)
        {
            return $"{resourceName}-nsg";
        }

        private static string GetOsDiskName(string resourceName)
        {
            return $"{resourceName}-disk";
        }
    }
}