﻿// <copyright file="LinuxVirtualMachineManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Create, update and deletes Azure virtual machines.
    /// </summary>
    public class LinuxVirtualMachineManager : IDeploymentManager
    {
        private const string Key = "value";
        private const string ExtensionName = "update-vm";
        private const string NicNameKey = "nicName";
        private const string NsgNameKey = "nsgName";
        private const string VnetNameKey = "vnetName";
        private const string DiskNameKey = "diskId";
        private const string QueueNameKey = "queueName";
        private const string VmNameKey = "vmName";
        private const string StorageAccountName = "vsocevmqueuestorage";
        private const string QueueResourceGroup = "vsocevmqueueresourcegroup";
        private static readonly string VmTemplateJson = GetVmTemplate();
        private readonly IAzureClientFactory clientFactory;
        private readonly IVSSaaSTokenProvider vmTokenProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxVirtualMachineManager"/> class.
        /// </summary>
        /// <param name="clientFactory">Builds Azure clients.</param>
        /// <param name="vmTokenProvider">VM Toke provider.</param>
        public LinuxVirtualMachineManager(IAzureClientFactory clientFactory, IVSSaaSTokenProvider vmTokenProvider)
        {
            Requires.NotNull(clientFactory, nameof(clientFactory));
            Requires.NotNull(vmTokenProvider, nameof(vmTokenProvider));

            this.clientFactory = clientFactory;
            this.vmTokenProvider = vmTokenProvider;
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
        {
            // create new VM resource name
            var resourceName = Guid.NewGuid().ToString();
            var vmToken = CreateVmToken(resourceName);

            var parameters = new Dictionary<string, Dictionary<string, object>>()
            {
                { "adminUserName", new Dictionary<string, object>() { { Key, "cloudenv" } } },
                { "adminPassword", new Dictionary<string, object>() { { Key, Guid.NewGuid() } } },
                { "vmSetupScript", new Dictionary<string, object>() { { Key, GetVmInitScript(vmToken) } } },
                { "location", new Dictionary<string, object>() { { Key, input.AzureVmLocation.ToString() } } },
                { "virtualMachineName", new Dictionary<string, object>() { { Key, resourceName} } },
                { "virtualMachineRG", new Dictionary<string, object>() { { Key, input.AzureResourceGroup } } },
                { "virtualMachineSize", new Dictionary<string, object>() { { Key, input.AzureSkuName } } },
                { "networkInterfaceName", new Dictionary<string, object>() { { Key, GetNetworkInterfaceName(resourceName) } } },
            };

            var deploymentName = $"Create-Vm-{resourceName}";
            try
            {
                IAzure azure = await clientFactory.GetAzureClientAsync(input.AzureSubscription);
                await azure.CreateResourceGroupIfNotExistsAsync(input.AzureResourceGroup, input.AzureVmLocation.ToString());

                // Create virtual machine
                IDeployment result = await azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(input.AzureResourceGroup)
                    .WithTemplate(VmTemplateJson)
                    .WithParameters(JsonConvert.SerializeObject(parameters))
                    .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                    .BeginCreateAsync();

                // Create input queue
                await azure.CreateResourceGroupIfNotExistsAsync(QueueResourceGroup, input.AzureVmLocation.ToString());
                await azure.CreateStorageAccountIfNotExistsAsync(QueueResourceGroup, input.AzureVmLocation.ToString(), StorageAccountName);
                var queue = await azure.GetCloudQueueClientAsync(QueueResourceGroup, StorageAccountName, GetQueueName(resourceName));
                var queueTrackingId = await queue.CreateIfNotExistsAsync();

                var azureResourceInfo = new AzureResourceInfo(input.AzureSubscription, input.AzureResourceGroup, resourceName);
                return (OperationState.InProgress, new NextStageInput(result.Name, azureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_begin_create_error", ex);
                return (OperationState.Failed, default);
            }
        }

        private string CreateVmToken(string input)
        {
            return this.vmTokenProvider.Generate(input);
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
                logger.LogException("linux_virtual_machine_manager_check_create_compute_error", ex);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1));
                }

                return (OperationState.Failed, default);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> BeginStartComputeAsync(VirtualMachineProviderStartComputeInput input, IDiagnosticsLogger logger)
        {
            var privateSettings = new Hashtable();
            privateSettings.Add("script", GetCustomScriptForVmAssign("vm_assign.sh", input));
            var parameters = new VirtualMachineExtensionUpdate()
            {
                ProtectedSettings = privateSettings,
                ForceUpdateTag = "true",
            };
            try
            {
                IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);
                var result = await computeClient.VirtualMachineExtensions.BeginUpdateAsync(
                    input.AzureResourceInfo.ResourceGroup,
                    input.AzureResourceInfo.Name,
                    ExtensionName,
                    parameters);

                return (OperationState.InProgress, new NextStageInput(result.Name, input.AzureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_begin_start_compute_error", ex);
                return (OperationState.Failed, default);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> CheckStartComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            try
            {
                IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);
                VirtualMachineExtensionInner result = await computeClient.VirtualMachineExtensions
                .GetAsync(
                    input.AzureResourceInfo.ResourceGroup,
                    input.AzureResourceInfo.Name,
                    input.TrackingId);
                return (ParseResult(result.ProvisioningState), new NextStageInput(input.TrackingId, input.AzureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_check_start_compute_error", ex);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1));
                }

                return (OperationState.Failed, default);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> BeginDeleteComputeAsync(VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            try
            {
                string vmName = input.AzureResourceInfo.Name;
                IAzure azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                string resourceGroup = input.AzureResourceInfo.ResourceGroup;
                IVirtualMachine linuxVM = await azure.VirtualMachines
                                  .GetByResourceGroupAsync(resourceGroup, vmName);

                OperationState vmDeletionState = OperationState.Succeeded;
                if (linuxVM != null)
                {
                    IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);
                    await computeClient.VirtualMachines.BeginDeleteAsync(resourceGroup, vmName);
                    vmDeletionState = OperationState.InProgress;
                }

                // Save resource state for continuation token.
                var resourcesToBeDeleted = new Dictionary<string, VmResourceState>();
                resourcesToBeDeleted.Add(VmNameKey, (vmName, vmDeletionState));
                resourcesToBeDeleted.Add(NicNameKey, (GetNetworkInterfaceName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(NsgNameKey, (GetNetworkSecurityGroupName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(VnetNameKey, (GetVnetName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(DiskNameKey, (GetOsDiskName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(QueueNameKey, (GetQueueName(vmName), OperationState.NotStarted));

                return (OperationState.InProgress, new NextStageInput(JsonConvert.SerializeObject(resourcesToBeDeleted), input.AzureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_begin_delete_error", ex);
                return (OperationState.Failed, default);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> CheckDeleteComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            try
            {
                Dictionary<string, VmResourceState> resourcesToBeDeleted = JsonConvert
                .DeserializeObject<Dictionary<string, VmResourceState>>(input.TrackingId);
                string resourceGroup = input.AzureResourceInfo.ResourceGroup;
                IAzure azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                INetworkManagementClient networkClient = await clientFactory.GetNetworkManagementClient(input.AzureResourceInfo.SubscriptionId);
                IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);

                if (resourcesToBeDeleted[VmNameKey].State != OperationState.Succeeded)
                {
                    // Check if virtual machine deletion is complete
                    IVirtualMachine linuxVM = await azure.VirtualMachines
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
                    (resourceName) => azure.DeleteQueueAsync(QueueResourceGroup, resourceName, StorageAccountName),
                    (resourceName) => QueueExistsAync(azure, QueueResourceGroup, resourceName, StorageAccountName),
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

                logger.LogErrorWithDetail("linux_virtual_machine_manager_check_delete_compute_error", s.ToString());
                NextStageInput nextStageInput = new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, nextStageInput);
                }

                return (OperationState.Failed, nextStageInput);
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

        private static string GetCustomScript(string scriptName)
        {
            string scriptString = GetEmbeddedResource(scriptName);
            return scriptString.ToBase64Encoded();
        }

        private static string GetCustomScriptForVmAssign(string scriptName, VirtualMachineProviderStartComputeInput input)
        {
            string scriptString = GetEmbeddedResource(scriptName);
            scriptString = AddParamsToScript(input, scriptString);
            return scriptString.ToBase64Encoded();
        }

        private static string AddParamsToScript(VirtualMachineProviderStartComputeInput input, string scriptString)
        {
            var camelCaseSerializer = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var storageParams = JsonConvert.SerializeObject(input.FileShareConnection, Formatting.None, camelCaseSerializer);
            scriptString = scriptString.Replace("SCRIPT_PARAM_STORAGE=''", $"SCRIPT_PARAM_STORAGE='{storageParams}'");
            var envParams = JsonConvert.SerializeObject(input.VmInputParams);
            scriptString = scriptString.Replace("SCRIPT_PARAM_CONTAINER_ENV_VARS=''", $"SCRIPT_PARAM_CONTAINER_ENV_VARS='{envParams}'");
            return scriptString;
        }

        private string GetVmInitScript(string vmToken)
        {
            var initScript = GetCustomScript("vm_init.sh");
            return initScript.Replace("SCRIPT_PARAM_VMTOKEN=''", $"SCRIPT_PARAM_VMTOKEN='{vmToken}'");
        }

        private static string GetEmbeddedResource(string resourceName)
        {
            string namespaceString = typeof(LinuxVirtualMachineManager).Namespace;
            var fullResourceName = $"{namespaceString}.Templates.Linux.{resourceName}";
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }

        private static string GetQueueName(string resourceName)
        {
            return $"{resourceName}-input-queue";
        }

        private static string GetOsDiskName(string vmName)
        {
            return $"{vmName}-disk";
        }

        private static string GetVnetName(string vmName)
        {
            return $"{vmName}-vnet";
        }

        private static string GetNetworkSecurityGroupName(string vmName)
        {
            return $"{vmName}-nsg";
        }

        private static string GetNetworkInterfaceName(string vmName)
        {
            return $"{vmName}-nic";
        }

        private static async Task<object> QueueExistsAync(IAzure azure, string resourceGroupName, string resourceName, string storageAccountName)
        {
            var queue = await azure.GetCloudQueueClientAsync(resourceGroupName, storageAccountName, resourceName);
            var queueExists = await queue.ExistsAsync();
            return queueExists ? new object() : default;
        }
    }
}