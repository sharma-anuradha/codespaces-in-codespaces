// <copyright file="WindowsVirtualMachineManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Create, update and deletes Azure virtual machines.
    /// </summary>
    public class WindowsVirtualMachineManager : VirtualMachineManagerBase
    {
        /// <summary>
        /// Name of the shim script which initializes a windows vm.
        /// The source of the shim script lives in the vsclk-cluster repository.
        /// When updated it release scripts for vsclk-cluster should be run on dev/ppe/prod to update the script on storage.
        /// Ideally no changes should be made to the shim script. Any init time additions should go to
        /// WindowsInit.ps1 in the Cascade repo.
        /// </summary>
        private const string WindowsInitShimScript = "WindowsInitShim.ps1";

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsVirtualMachineManager"/> class.
        /// </summary>
        /// <param name="clientFactory">Builds Azure clients.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Control plane azure accessor.</param>
        public WindowsVirtualMachineManager(
            IAzureClientFactory clientFactory,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
            : base(clientFactory, controlPlaneAzureResourceAccessor)
        {
        }

        /// <inheritdoc/>
        public override bool Accepts(ComputeOS computeOS)
        {
            return computeOS == ComputeOS.Windows;
        }

        /// <inheritdoc/>
        public override async Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
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
                var azure = await ClientFactory.GetAzureClientAsync(input.AzureSubscription);
                await azure.CreateResourceGroupIfNotExistsAsync(input.AzureResourceGroup, input.AzureVmLocation.ToString());

                // Create input queue
                string inputQueueName = GetInputQueueName(virtualMachineName);
                var inputQueueConnectionInfo = await CreateQueue(input, logger, virtualMachineName, inputQueueName);

                // Get information about the storage account to pass into the custom script.
                var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeVmAgentImagesAsync(input.AzureVmLocation);
                var storageAccountName = storageInfo.Item1;
                var storageAccountAccessKey = storageInfo.Item2;
                var vmInitScriptFileUri = $"https://{storageAccountName}.blob.core.windows.net/windows-init-shim/{WindowsInitShimScript}";
                var userName = "vsonline";

                // Required parameters forwarded to the VM agent init script.
                // Be very careful removing parameters from this list because it can break the VM agent init script.
                var initScriptParametersBlob = new Dictionary<string, object>()
                {
                    { "inputQueueName", inputQueueConnectionInfo.Name },
                    { "inputQueueUrl", inputQueueConnectionInfo.Url },
                    { "inputQueueSasToken", inputQueueConnectionInfo.SasToken },
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
                    { "imageReferenceId", new Dictionary<string, object>() { { Key, input.AzureVirtualMachineImage } } },
                    { "virtualMachineRG", new Dictionary<string, object>() { { Key, input.AzureResourceGroup } } },
                    { "virtualMachineName", new Dictionary<string, object>() { { Key, virtualMachineName } } },
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
        protected override string GetVmTemplate()
        {
            var resourceName = "template_vm.json";
            var namespaceString = typeof(WindowsVirtualMachineManager).Namespace;
            var fullyQualifiedResourceName = $"{namespaceString}.Templates.Windows.{resourceName}";
            return CommonUtils.GetEmbeddedResourceContent(fullyQualifiedResourceName);
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
    }
}