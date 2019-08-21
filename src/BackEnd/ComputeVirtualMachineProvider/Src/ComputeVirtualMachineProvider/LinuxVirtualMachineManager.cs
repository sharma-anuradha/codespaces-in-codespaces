// <copyright file="LinuxVirtualMachineManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
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
        private const string VmNameKey = "vmName";
        private static readonly string VmTemplateJson = GetVmTemplate();
        private static readonly string VmInitScript = GetCustomScript("vm_init.sh");
        private readonly IAzureClientFactory clientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxVirtualMachineManager"/> class.
        /// </summary>
        /// <param name="clientFactory">Builds Azure clients.</param>
        public LinuxVirtualMachineManager(IAzureClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        /// <inheritdoc/>
        public async Task<(DeploymentState, DeploymentStatusInput)> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input)
        {
            // create new resource id
            var resourceId = new Common.Models.ResourceId(ResourceType.ComputeVM, Guid.NewGuid(), input.AzureSubscription, input.AzureResourceGroup, input.AzureVmLocation);
            IAzure azure = await clientFactory.GetAzureClientAsync(input.AzureSubscription);
            var parameters = new Dictionary<string, Dictionary<string, object>>()
            {
                { "adminUserName", new Dictionary<string, object>() { { Key, "cloudenv" } } },
                { "adminPassword", new Dictionary<string, object>() { { Key, Guid.NewGuid() } } }, // TODO:: Make it more secure
                { "vmSetupScript", new Dictionary<string, object>() { { Key, VmInitScript } } }, // TODO:: pipe from config, cloudinit script to deploy docker
                { "location", new Dictionary<string, object>() { { Key, input.AzureVmLocation.ToString() } } },
                { "virtualMachineName", new Dictionary<string, object>() { { Key, resourceId.InstanceId.ToString() } } },
                { "virtualMachineRG", new Dictionary<string, object>() { { Key, input.AzureResourceGroup } } },
                { "virtualMachineSize", new Dictionary<string, object>() { { Key, input.AzureSkuName } } },
                { "networkInterfaceName", new Dictionary<string, object>() { { Key, $"{resourceId.InstanceId}-nic" } } },
            };

            var deploymentName = $"Create-Vm-{resourceId.InstanceId}";

            IDeployment result = await azure.Deployments.Define(deploymentName)
                .WithExistingResourceGroup(input.AzureResourceGroup)
                .WithTemplate(VmTemplateJson)
                .WithParameters(JsonConvert.SerializeObject(parameters))
                .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                .BeginCreateAsync();

            return (DeploymentState.InProgress, new DeploymentStatusInput(result.Name, resourceId));
        }

        /// <inheritdoc/>
        public async Task<(DeploymentState, DeploymentStatusInput)> BeginStartComputeAsync(VirtualMachineProviderStartComputeInput input)
        {
            IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.ResourceId.SubscriptionId);
            var privateSettings = new Hashtable();
            privateSettings.Add("script", GetCustomScriptForVmAssign("vm_assign.sh", input));
            var parameters = new VirtualMachineExtensionUpdate()
            {
                ProtectedSettings = privateSettings,
                ForceUpdateTag = "true",
            };

            var result = await computeClient.VirtualMachineExtensions.BeginUpdateAsync(
                input.ResourceId.ResourceGroup,
                input.ResourceId.InstanceId.ToString(),
                ExtensionName,
                parameters);

            return (DeploymentState.InProgress, new DeploymentStatusInput(result.Name, input.ResourceId));
        }

        /// <inheritdoc/>
        public async Task<(DeploymentState, DeploymentStatusInput)> CheckStartComputeStatusAsync(DeploymentStatusInput input)
        {
            IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.ResourceId.SubscriptionId);
            VirtualMachineExtensionInner result = await computeClient.VirtualMachineExtensions
            .GetAsync(
                input.ResourceId.ResourceGroup,
                input.ResourceId.InstanceId.ToString(),
                input.TrackingId);
            return (ParseResult(result.ProvisioningState), input);
        }

        /// <inheritdoc/>
        public async Task<(DeploymentState, DeploymentStatusInput)> CheckCreateComputeStatusAsync(DeploymentStatusInput deploymentStatusInput)
        {
            IAzure azure = await clientFactory.GetAzureClientAsync(deploymentStatusInput.ResourceId.SubscriptionId);
            IDeployment deployment = await azure.Deployments.GetByResourceGroupAsync(deploymentStatusInput.ResourceId.ResourceGroup, deploymentStatusInput.TrackingId);

            return (ParseResult(deployment.ProvisioningState), deploymentStatusInput);
        }

        /// <inheritdoc/>
        public async Task<(DeploymentState, DeploymentStatusInput)> BeginDeleteComputeAsync(VirtualMachineProviderDeleteInput input)
        {
            string vmName = input.ResourceId.InstanceId.ToString();
            IAzure azure = await clientFactory.GetAzureClientAsync(input.ResourceId.SubscriptionId);
            string resourceGroup = input.ResourceId.ResourceGroup;
            IVirtualMachine linuxVM = await azure.VirtualMachines
                              .GetByResourceGroupAsync(resourceGroup, vmName);
            if (linuxVM == null)
            {
                return (DeploymentState.Succeeded, new DeploymentStatusInput(default, input.ResourceId));
            }

            IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.ResourceId.SubscriptionId);
            await computeClient.VirtualMachines.BeginDeleteAsync(resourceGroup, vmName);

            // Get disk to get disk name
            var disk = await azure.Disks.GetByIdAsync(linuxVM.OSDiskId);

            // Save resource state for continuation token.
            var resourcesToBeDeleted = new Dictionary<string, VmResourceState>
            {
                { VmNameKey,  (linuxVM.Name, DeploymentState.InProgress) },
                { DiskNameKey,  (disk.Name, DeploymentState.Pending) },
                { NicNameKey,  ($"{vmName}-nic", DeploymentState.Pending) },
                { NsgNameKey,  ($"{vmName}-nsg", DeploymentState.Pending) },
                { VnetNameKey, ($"{vmName}-vnet", DeploymentState.Pending) },
            };

            return (DeploymentState.InProgress, new DeploymentStatusInput(JsonConvert.SerializeObject(resourcesToBeDeleted), input.ResourceId));
        }

        /// <inheritdoc/>
        public async Task<(DeploymentState, DeploymentStatusInput)> CheckDeleteComputeStatusAsync(DeploymentStatusInput input)
        {
            Dictionary<string, VmResourceState> resourcesToBeDeleted = JsonConvert
                .DeserializeObject<Dictionary<string, VmResourceState>>(input.TrackingId);
            string resourceGroup = input.ResourceId.ResourceGroup;
            IAzure azure = await clientFactory.GetAzureClientAsync(input.ResourceId.SubscriptionId);
            INetworkManagementClient networkClient = await clientFactory.GetNetworkManagementClient(input.ResourceId.SubscriptionId);
            IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.ResourceId.SubscriptionId);

            if (resourcesToBeDeleted[VmNameKey].State != DeploymentState.Succeeded)
            {
                // Check if virtual machine deletion is complete
                IVirtualMachine linuxVM = await azure.VirtualMachines
                          .GetByResourceGroupAsync(resourceGroup, resourcesToBeDeleted[VmNameKey].Name);
                if (linuxVM != null)
                {
                    return (DeploymentState.InProgress, input);
                }
                else
                {
                    resourcesToBeDeleted[VmNameKey] = (resourcesToBeDeleted[VmNameKey].Name, DeploymentState.Succeeded);
                }
            }

            // Virtual machine is deleted, delete the remaining resources
            var taskList = new List<Task>();
            try
            {
                taskList.Add(
                    CheckResourceStatus(
                      (resourceName) => networkClient.NetworkInterfaces.BeginDeleteAsync(resourceGroup, resourceName),
                      (resourceName) => azure.NetworkInterfaces.GetByResourceGroupAsync(resourceGroup, resourceName),
                      resourcesToBeDeleted,
                      NicNameKey));

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

                taskList.Add(
                CheckResourceStatus(
                    (resourceName) => computeClient.Disks.BeginDeleteAsync(resourceGroup, resourceName),
                    (resourceName) => azure.Disks.GetByResourceGroupAsync(resourceGroup, resourceName),
                    resourcesToBeDeleted,
                    DiskNameKey));

                await Task.WhenAll(taskList);
            }
            catch (Exception ex)
            {
                // TODO: Parse exception and mark resource state
                // log exception
                throw;
            }

            var deploymentStausInput = new DeploymentStatusInput(JsonConvert.SerializeObject(resourcesToBeDeleted), input.ResourceId);
            var resultState = GetFinalState(resourcesToBeDeleted);
            return (resultState, deploymentStausInput);
        }

        private static DeploymentState GetFinalState(Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            if (resourcesToBeDeleted.Any(r => r.Value.State == DeploymentState.InProgress))
            {
                return DeploymentState.InProgress;
            }

            if (resourcesToBeDeleted.Any(r => r.Value.State == DeploymentState.Failed))
            {
                return DeploymentState.Failed;
            }

            if (resourcesToBeDeleted.Any(r => r.Value.State == DeploymentState.Cancelled))
            {
                return DeploymentState.Cancelled;
            }

            return DeploymentState.Succeeded;
        }

        private static Task CheckResourceStatus<TResult>(
           Func<string, Task> deleteResourceFunc,
           Func<string, Task<TResult>> checkResourceFunc,
           Dictionary<string, VmResourceState> resourcesToBeDeleted,
           string resourceNameKey)
        {
            string resourceName = resourcesToBeDeleted[resourceNameKey].Name;
            DeploymentState resourceState = resourcesToBeDeleted[resourceNameKey].State;
            if (resourceState == DeploymentState.Pending)
            {
                Task beginDeleteTask = deleteResourceFunc(resourceName);
                return beginDeleteTask.ContinueWith(
                        (task) =>
                    {
                        if (task.IsCompleted)
                        {
                            resourcesToBeDeleted[resourceNameKey] = (resourceName, DeploymentState.InProgress);
                        }
                        else if (task.IsFaulted)
                        {
                            resourcesToBeDeleted[resourceNameKey] = (resourceName, DeploymentState.Failed);
                        }
                        else if (task.IsCanceled)
                        {
                            resourcesToBeDeleted[resourceNameKey] = (resourceName, DeploymentState.Cancelled);
                        }
                    });
            }
            else if (resourceState == DeploymentState.InProgress)
            {
                Task<TResult> checkStatusTask = checkResourceFunc(resourceName);
                return checkStatusTask.ContinueWith(
                        (task) =>
                     {
                         if (task.IsCompleted && task.Result == null)
                         {
                             resourcesToBeDeleted[resourceNameKey] = (resourceName, DeploymentState.Succeeded);
                         }
                     });
            }

            return Task.CompletedTask;
        }

        private static DeploymentState ParseResult(string provisioningState)
        {
            if (provisioningState.Equals(DeploymentState.Succeeded.ToString(), StringComparison.OrdinalIgnoreCase)
           || provisioningState.Equals(DeploymentState.Failed.ToString(), StringComparison.OrdinalIgnoreCase)
           || provisioningState.Equals(DeploymentState.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return provisioningState.ToEnum<DeploymentState>();
            }

            return DeploymentState.InProgress;
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

        private static string GetEmbeddedResource(string resourceName)
        {
            string namespaceString = typeof(LinuxVirtualMachineManager).Namespace;
            var fullResourceName = $"{namespaceString}.Templates.{resourceName}";
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }
    }
}