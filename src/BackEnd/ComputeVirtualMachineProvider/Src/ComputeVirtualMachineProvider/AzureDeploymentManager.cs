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
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;

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
        public async Task<DeploymentStatusInput> BeginCreateAsync(VirtualMachineInstance vmInstance)
        {
            IAzure azure = clientFactory.GetAzureClient(vmInstance.AzureSubscription);
            var parameters = new Dictionary<string, Dictionary<string, object>>()
            {
                { "adminUserName", new Dictionary<string, object>() { { Key, "cloudenv" } } },
                { "adminPassword", new Dictionary<string, object>() { { Key, Guid.NewGuid() } } }, // TODO:: Make it more secure
                { "customData", new Dictionary<string, object>() { { Key, CloudInitScript } } }, // TODO:: pipe from config, cloudinit script to deploy docker
                { "location", new Dictionary<string, object>() { { Key, vmInstance.AzureLocation.ToString() } } },
                { "virtualMachineName", new Dictionary<string, object>() { { Key, vmInstance.AzureInstanceId.ToString() } } },
                { "virtualMachineRG", new Dictionary<string, object>() { { Key, vmInstance.AzureResourceGroupName } } },
                { "virtualMachineSize", new Dictionary<string, object>() { { Key, vmInstance.AzureSku } } },
                { "networkInterfaceName", new Dictionary<string, object>() { { Key, $"{vmInstance.AzureInstanceId}-nic" } } },
            };

            var deploymentName = $"Create-{vmInstance.AzureInstanceId}";

            var resourceGroup = azure.ResourceGroups.Define(vmInstance.AzureResourceGroupName)
                .WithRegion(vmInstance.AzureLocation.ToString())
                .Create();

            IDeployment result = await azure.Deployments.Define(deploymentName)
                .WithExistingResourceGroup(vmInstance.AzureResourceGroupName)
                .WithTemplate(VmTemplateJson)
                .WithParameters(JsonConvert.SerializeObject(parameters))
                .WithMode(Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                .BeginCreateAsync()
                .ContinueOnAnyContext();

            return new DeploymentStatusInput(vmInstance.AzureSubscription, vmInstance.AzureResourceGroupName, result.Name, vmInstance.GetResourceId());
        }

        /// <inheritdoc/>
        public async Task<DeploymentState> CheckDeploymentStatusAsync(DeploymentStatusInput deploymentStatusInput)
        {
            IAzure azure = clientFactory.GetAzureClient(deploymentStatusInput.ResourceId.SubscriptionId);
            IDeployment deployment = await azure.Deployments.GetByResourceGroupAsync(deploymentStatusInput.AzureResourceGroupName, deploymentStatusInput.AzureDeploymentName).ContinueOnAnyContext();
            DeploymentState deploymentState = deployment.ProvisioningState.ToEnum<DeploymentState>();

            if (deploymentState == DeploymentState.Succeeded
            || deploymentState == DeploymentState.Failed
            || deploymentState == DeploymentState.Cancelled)
            {
                return deploymentState;
            }
            return DeploymentState.InProgress;
        }

        /// <inheritdoc/> 
        public async Task<DeploymentState> DeleteVMAsync(ResourceId resourceId)
        {
            IAzure azure = clientFactory.GetAzureClient(resourceId.SubscriptionId);
            await azure.ResourceGroups.BeginDeleteByNameAsync(resourceId.ResourceGroup).ContinueOnAnyContext();
            return DeploymentState.Succeeded;
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

        public static string VmTemplateJson => vmTemplateJson;

        public static string CloudInitScript => cloudInitScript;
    }
}