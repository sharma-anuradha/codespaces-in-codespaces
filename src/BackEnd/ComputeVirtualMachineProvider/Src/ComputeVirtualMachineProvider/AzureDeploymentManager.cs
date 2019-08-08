// <copyright file="AzureDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class AzureDeploymentManager : IDeploymentManager
    {
        public AzureDeploymentManager(IAzureClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        private const string Key = "value";

        /// <inheritdoc/>
        public async Task<DeploymentStatusInput> BeginCreateAsync(VirtualMachineProviderCreateInput input)
        {
            // create new resource id
            ResourceId resourceId = new ResourceId(ResourceType.ComputeVM, Guid.NewGuid(), input.AzureSubscription, input.AzureResourceGroup, input.AzureVmLocation);
            IAzure azure = clientFactory.GetAzureClient(input.AzureSubscription);
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

            var deploymentName = $"Create-{resourceId.InstanceId}";

            IDeployment result = await azure.Deployments.Define(deploymentName)
                .WithExistingResourceGroup(input.AzureResourceGroup)
                .WithTemplate(VmTemplateJson)
                .WithParameters(JsonConvert.SerializeObject(parameters))
                .WithMode(Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                .BeginCreateAsync()
                .ContinueOnAnyContext();

            return new DeploymentStatusInput(result.Name, resourceId);
        }

        /// <inheritdoc/>
        public async Task<DeploymentStatusInput> BeginAllocateAsync(VirtualMachineProviderAllocateInput input)
        {
            IAzure azure = clientFactory.GetAzureClient(input.ResourceId.SubscriptionId);
            IVirtualMachine linuxVM = await azure.VirtualMachines
                            .GetByResourceGroupAsync(input.ResourceId.ResourceGroup, input.ResourceId.InstanceId.ToString())
                            .ContinueOnAnyContext();

            string name = $"Assign-{input.ResourceId.InstanceId}";
            IVirtualMachine result = await linuxVM.Update()
                          .UpdateExtension("config-app")
                          .WithProtectedSetting("script", VMAssignScript)
                          .WithMinorVersionAutoUpgrade()
                          .Parent()
                          .ApplyAsync()
                          .ContinueOnAnyContext();

            return new DeploymentStatusInput(name, input.ResourceId);
        }

        /// <inheritdoc/>
        public async Task<DeploymentState> CheckDeploymentStatusAsync(DeploymentStatusInput deploymentStatusInput)
        {
            IAzure azure = clientFactory.GetAzureClient(deploymentStatusInput.ResourceId.SubscriptionId);
            IDeployment deployment = await azure.Deployments.GetByResourceGroupAsync(deploymentStatusInput.AzureResourceGroupName, deploymentStatusInput.AzureDeploymentName).ContinueOnAnyContext();

            if (deployment.ProvisioningState.Equals(DeploymentState.Succeeded.ToString(), StringComparison.OrdinalIgnoreCase)
            || deployment.ProvisioningState.Equals(DeploymentState.Failed.ToString(), StringComparison.OrdinalIgnoreCase)
            || deployment.ProvisioningState.Equals(DeploymentState.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return deployment.ProvisioningState.ToEnum<DeploymentState>();
            }
            return DeploymentState.InProgress;
        }

        private static string GetVmTemplate()
        {
            return GetEmbeddedResource("template_vm.json");
        }

        private static string GetCloundInitScript()
        {
            string cloudInitScriptString = GetEmbeddedResource("cloudinit.yml");
            return cloudInitScriptString.ToBase64Encoded();
        }

        private static string GetCustomScript(string scriptName)
        {
            string cloudInitScriptString = GetEmbeddedResource(scriptName);
            return cloudInitScriptString.ToBase64Encoded();
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
        private static readonly string vmTemplateJson = GetVmTemplate();
        private static readonly string cloudInitScript = GetCloundInitScript();
        private readonly IAzureClientFactory clientFactory;
        private static readonly string vmInitScript = GetCustomScript("vm_init.sh");
        private static readonly string vmAssignScript = GetCustomScript("vm_assign.sh");
        public static string VmTemplateJson => vmTemplateJson;
        public static string CloudInitScript => cloudInitScript;
        public static string VMInitScript => vmInitScript;
        public static string VMAssignScript => vmAssignScript;
    }
}