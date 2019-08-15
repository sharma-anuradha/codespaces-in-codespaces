// <copyright file="AzureDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class AzureDeploymentManager : IDeploymentManager
    {
        private const string Key = "value";
        private const string ExtensionName = "update-vm";
        private const string ExtensionType = "CustomScript";
        private const string ExtensionPublisher = "Microsoft.Compute";
        private static readonly string VmTemplateJsonValue = GetVmTemplate();
        private static readonly string vmInitScript = GetCustomScript("vm_init.sh");
        private readonly IAzureClientFactory clientFactory;

        public AzureDeploymentManager(IAzureClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        /// <inheritdoc/>
        public async Task<DeploymentStatusInput> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input)
        {
            // create new resource id
            ResourceId resourceId = new ResourceId(ResourceType.ComputeVM, Guid.NewGuid(), input.AzureSubscription, input.AzureResourceGroup, input.AzureVmLocation);
            IAzure azure = await clientFactory.GetAzureClientAsync(input.AzureSubscription);
            var parameters = new Dictionary<string, Dictionary<string, object>>()
            {
                { "adminUserName", new Dictionary<string, object>() { { Key, "cloudenv" } } },
                { "adminPassword", new Dictionary<string, object>() { { Key, Guid.NewGuid() } } }, // TODO:: Make it more secure
                { "vmSetupScript", new Dictionary<string, object>() { { Key, VMInitScript } } }, // TODO:: pipe from config, cloudinit script to deploy docker
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

            return new DeploymentStatusInput(result.Name, resourceId);
        }

        /// <inheritdoc/>
        public async Task<DeploymentStatusInput> BeginStartComputeAsync(VirtualMachineProviderStartComputeInput input)
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

            return new DeploymentStatusInput(result.Name, input.ResourceId);
        }

        /// <inheritdoc/>
        public async Task<DeploymentState> CheckStartComputeStatusAsync(DeploymentStatusInput input)
        {
            IComputeManagementClient computeClient = await clientFactory.GetComputeManagementClient(input.ResourceId.SubscriptionId);
            VirtualMachineExtensionInner result = await computeClient.VirtualMachineExtensions
            .GetAsync(
                input.ResourceId.ResourceGroup,
                input.ResourceId.InstanceId.ToString(),
                input.TrackingId);
            return ParseResult(result.ProvisioningState);
        }

        /// <inheritdoc/>
        public async Task<DeploymentState> CheckCreateComputeStatusAsync(DeploymentStatusInput deploymentStatusInput)
        {
            IAzure azure = await clientFactory.GetAzureClientAsync(deploymentStatusInput.ResourceId.SubscriptionId);
            IDeployment deployment = await azure.Deployments.GetByResourceGroupAsync(deploymentStatusInput.ResourceId.ResourceGroup, deploymentStatusInput.TrackingId);

            return ParseResult(deployment.ProvisioningState);
        }

        /// <inheritdoc/>
        public async Task<DeploymentStatusInput> BeginDeleteComputeAsync(VirtualMachineProviderDeleteInput input)
        {
            // TODO:: move to begin wait pattern
            string vmName = input.ResourceId.InstanceId.ToString();
            IAzure azure = await clientFactory.GetAzureClientAsync(input.ResourceId.SubscriptionId);
            IVirtualMachine linuxVM = await azure.VirtualMachines
                              .GetByResourceGroupAsync(input.ResourceId.ResourceGroup, vmName);

            if (linuxVM == null)
            {
                return default;
            }

            string diskId = linuxVM.OSDiskId;
            string nicName = $"{vmName}-nic";
            string nsgName = $"{vmName}-nsg";
            string vnetName = $"{vmName}-vnet";
            string rgName = linuxVM.ResourceGroupName;

            // TODO:: Migrate to new IAAS API that deletes all vm resources along with vm.
            // ref: https://feedback.azure.com/forums/216843-virtual-machines/suggestions/8945692-delete-vm-with-all-associated-resources
            await azure.VirtualMachines
                .DeleteByIdAsync(linuxVM.Id);

            await azure.NetworkInterfaces
                .DeleteByResourceGroupAsync(rgName, nicName);

            await azure.NetworkSecurityGroups
               .DeleteByResourceGroupAsync(rgName, nsgName);

            await azure.VirtualNetworkGateways
             .DeleteByResourceGroupAsync(rgName, vnetName);

            await azure.Disks
                .DeleteByIdAsync(diskId);

            return default;
        }

        /// <inheritdoc/>
        public async Task<DeploymentState> CheckDeleteComputeStatusAsync(DeploymentStatusInput arg)
        {
            // TODO:: move to begin wait pattern
            return await Task.FromResult(DeploymentState.Succeeded);
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
            string namespaceString = typeof(AzureDeploymentManager).Namespace;
            var fullResourceName = $"{namespaceString}.Templates.{resourceName}";
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }

        public static string VmTemplateJson => VmTemplateJsonValue;

        public static string VMInitScript => vmInitScript;
    }
}