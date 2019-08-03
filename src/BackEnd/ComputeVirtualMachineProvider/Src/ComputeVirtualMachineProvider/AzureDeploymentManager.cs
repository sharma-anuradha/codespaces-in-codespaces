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
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class AzureDeploymentManager : IDeploymentManager
    {
        /// <inheritdoc/>
        public async Task<DeploymentStatusInput> BeginCreateAsync(VirtualMachineInstance vmInstance)
        {
            IAzure azure = GetAzureClient();
            Dictionary<string, Dictionary<string, object>> parameters = new Dictionary<string, Dictionary<string, object>>()
            {
                { "adminUserName", new Dictionary<string, object>() { { "value", "cloudenv" } } },
                { "adminPassword", new Dictionary<string, object>() { { "value", Guid.NewGuid() } } }, // TODO:: Make it more secure
                { "customData", new Dictionary<string, object>() { { "value", CloudInitScript } } }, // TODO:: pipe from config, cloudinit script to deploy docker
                { "location", new Dictionary<string, object>() { { "value", vmInstance.AzureLocation } } },
                { "virtualMachineName", new Dictionary<string, object>() { { "value", vmInstance.AzureResourceName } } },
                { "virtualMachineRG", new Dictionary<string, object>() { { "value", vmInstance.AzureResourceGroupName } } },
                { "virtualMachineSize", new Dictionary<string, object>() { { "value", vmInstance.AzureSku } } },
                { "networkInterfaceName", new Dictionary<string, object>() { { "value", $"{vmInstance.AzureResourceName}-nic" } } },
            };

            var deploymentName = $"Create{vmInstance.AzureResourceName}-{Guid.NewGuid()}";

            IDeployment result = await azure.Deployments.Define(deploymentName)
                .WithExistingResourceGroup(vmInstance.AzureResourceGroupName)
                .WithTemplate(VmTemplateJson)
                .WithParameters(JsonConvert.SerializeObject(parameters))
                .WithMode(Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                .BeginCreateAsync().ContinueOnAnyContext();

            return new DeploymentStatusInput(vmInstance.AzureSubscription, vmInstance.AzureResourceGroupName, result.Name, vmInstance.GetResourceId());
        }

        /// <inheritdoc/>
        public async Task<DeploymentState> CheckDeploymentStatusAsync(DeploymentStatusInput deploymentStatusInput)
        {
            try
            {
                IAzure azure = GetAzureClient();
                IDeployment deployment = await azure.Deployments.GetByResourceGroupAsync(deploymentStatusInput.AzureResourceGroupName, deploymentStatusInput.AzureDeploymentName).ContinueOnAnyContext();
                if (!(StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Succeeded") ||
                            StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Failed") ||
                            StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Cancelled")))
                {
                    return DeploymentState.InProgress;
                }

                DeploymentState deploymentState = deployment.ProvisioningState.ToEnum<DeploymentState>();

                if (deploymentState == DeploymentState.Succeeded)
                {
                    await azure.Deployments.DeleteByResourceGroupAsync(deploymentStatusInput.AzureResourceGroupName, deploymentStatusInput.AzureDeploymentName).ContinueOnAnyContext();
                }

                return deploymentState;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get the VM Deployment status", ex);
            }
        }

        private static string GetVmTemplate()
        {
            var resourceName = "Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Templates.template_vm.json";
            return GetEmbeddedResource(resourceName);
        }

        private static string GetCloundInitScript()
        {
            var resourceName = "Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Templates.cloudinit.yml";
            string cloudInitScriptString = GetEmbeddedResource(resourceName);
            return cloudInitScriptString.ToBase64Encoded();
        }

        private static string GetEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }

        private static IAzure GetAzureClient()
        {
            string computeAzureAppId = null;
            string computeAzureAppKey = null;
            string computeAzureTenant = null;
            string computeAzureSubscriptionId = null;
            var creds = new AzureCredentialsFactory().FromServicePrincipal(computeAzureAppId, computeAzureAppKey, computeAzureTenant, AzureEnvironment.AzureGlobalCloud);
            return Azure.Management.Fluent.Azure.Authenticate(creds).WithSubscription(computeAzureSubscriptionId);
        }

        private static readonly string vmTemplateJson = GetVmTemplate();
        private static readonly string cloudInitScript = GetCloundInitScript();

        public static string VmTemplateJson => vmTemplateJson;

        public static string CloudInitScript => cloudInitScript;
    }
}
