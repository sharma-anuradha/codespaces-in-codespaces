// <copyright file="WindowsVirtualMachineStrategyBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies
{
    /// <summary>
    /// base windows vm creation strategy.
    /// </summary>
    public abstract class WindowsVirtualMachineStrategyBase : ICreateVirtualMachineStrategy
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
        /// Initializes a new instance of the <see cref="WindowsVirtualMachineStrategyBase"/> class.
        /// </summary>
        /// <param name="clientFactory">client factory.</param>
        /// <param name="queueProvider">queue provider.</param>
        /// <param name="templateName">vm template name.</param>
        /// <param name="controlPlaneAzureResourceAccessor">control plane azure resource accessor.</param>
        public WindowsVirtualMachineStrategyBase(
            IAzureClientFactory clientFactory,
            IQueueProvider queueProvider,
            string templateName,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            ClientFactory = clientFactory;
            QueueProvider = queueProvider;
            ControlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
            VirtualMachineTemplateJson = GetVmTemplate(templateName);
        }

        /// <inheritdoc/>
        public string VirtualMachineTemplateJson { get; }

        /// <summary>
        /// Gets client factory.
        /// </summary>
        protected IAzureClientFactory ClientFactory { get; }

        /// <summary>
        /// Gets queue provider.
        /// </summary>
        protected IQueueProvider QueueProvider { get; }

        private IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; }

        /// <inheritdoc/>
        public abstract bool Accepts(VirtualMachineProviderCreateInput input);

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateVirtualMachine(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
        {
            Requires.NotNull(input.AzureResourceGroup, nameof(input.AzureResourceGroup));
            Requires.NotNull(input.AzureSkuName, nameof(input.AzureSkuName));
            Requires.NotNull(input.AzureVirtualMachineImage, nameof(input.AzureVirtualMachineImage));
            Requires.NotNull(input.VMToken, nameof(input.VMToken));
            Requires.NotNull(input.QueueConnectionInfo, nameof(input.QueueConnectionInfo));

            // create new VM resource name
            var virtualMachineName = GetVirtualMachineName();

            var resourceTags = input.ResourceTags;
            VirtualMachineDeploymentManager.UpdateResourceTags(input.CustomComponents, virtualMachineName, resourceTags);

            var deploymentName = $"Create-WindowsVm-{virtualMachineName}";
            try
            {
                var azure = await ClientFactory.GetAzureClientAsync(input.AzureSubscription);
                await azure.CreateResourceGroupIfNotExistsAsync(input.AzureResourceGroup, input.AzureVmLocation.ToString());

                // Get information about the storage account to pass into the custom script.
                var storageInfo = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeVmAgentImagesAsync(input.AzureVmLocation);
                var storageAccountName = storageInfo.Item1;
                var storageAccountAccessKey = storageInfo.Item2;
                var vmInitScriptFileUri = $"https://{storageAccountName}.blob.core.windows.net/windows-init-shim/{WindowsInitShimScript}";
                var userName = "vsonline";

                // Required parameters forwarded to the VM agent init script.
                // Be very careful removing parameters from this list because it can break the VM agent init script.
                var initScriptParametersBlob = input.GenerateInitScriptParametersBlob(@"C:\VisualStudio", userName);

                var parameters = await GetVMParametersAsync(
                     input,
                     virtualMachineName,
                     resourceTags,
                     storageAccountName,
                     storageAccountAccessKey,
                     vmInitScriptFileUri,
                     userName,
                     initScriptParametersBlob);

                await PreCreateTaskAsync(input, logger);

                // Create virtual machine
                var result = await DeploymentUtils.BeginCreateArmResource(
                    input.AzureResourceGroup,
                    azure,
                    VirtualMachineTemplateJson,
                    parameters,
                    deploymentName);

                var azureResourceInfo = new AzureResourceInfo(input.AzureSubscription, input.AzureResourceGroup, virtualMachineName);

                return (OperationState.InProgress, new NextStageInput(result.Name, azureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("windows_virtual_machine_manager_begin_create_error", ex);
                throw;
            }
        }

        /// <summary>
        /// Check if input contains OSDisk.
        /// </summary>
        /// <param name="input">input.</param>
        /// <returns>result.</returns>
        protected static bool CreateWithOSDisk(VirtualMachineProviderCreateInput input)
        {
            return input.CustomComponents != default
                && input.CustomComponents.Any(x =>
                                           x.ComponentType == ResourceType.OSDisk &&
                                           !string.IsNullOrEmpty(x.AzureResourceInfo?.Name));
        }

        /// <summary>
        /// Check if input contains Nic.
        /// </summary>
        /// <param name="input">input.</param>
        /// <returns>result.</returns>
        protected static bool CreateWithNic(VirtualMachineProviderCreateInput input)
        {
            return input.CustomComponents != default
                && input.CustomComponents.Any(c => c.ComponentType == ResourceType.NetworkInterface);
        }

        /// <summary>
        /// Encode script parameters.
        /// </summary>
        /// <param name="initScriptParametersBlob">parameters.</param>
        /// <returns>result.</returns>
        protected static string EncodeScriptParameters(IDictionary<string, object> initScriptParametersBlob)
        {
            // b64 encode the parameters json blob, this gets forwarded onto the VM agent script through the custom script extension.
            var parametersBlob = JsonConvert.SerializeObject(initScriptParametersBlob);
            var encodedBytes = Encoding.UTF8.GetBytes(parametersBlob);
            var b64ParametersBlob = Convert.ToBase64String(encodedBytes);
            return b64ParametersBlob;
        }

        /// <summary>
        /// Validate OS Disk.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="osDiskInfo">os disk info.</param>
        /// <returns>result.</returns>
        protected async Task<IDisk> ValidateOSDisk(VirtualMachineProviderCreateInput input, AzureResourceInfo osDiskInfo)
        {
            var azure = await ClientFactory.GetAzureClientAsync(input.AzureSubscription);
            var disk = await azure.Disks.GetByResourceGroupAsync(osDiskInfo.ResourceGroup, osDiskInfo.Name);

            if (disk.IsAttachedToVirtualMachine)
            {
                throw new InvalidOperationException("Should not try to use a disk which is already attached.");
            }

            return disk;
        }

        /// <summary>
        /// Get template parameters.
        /// </summary>
        /// <param name="input">vm input.</param>
        /// <param name="virtualMachineName">vm name.</param>
        /// <param name="resourceTags">resource tags.</param>
        /// <param name="storageAccountName">storageAccountName.</param>
        /// <param name="storageAccountAccessKey">storageAccountAccessKey.</param>
        /// <param name="vmInitScriptFileUri">vmInitScriptFileUri.</param>
        /// <param name="userName">userName.</param>
        /// <param name="initScriptParametersBlob">script parameters.</param>
        /// <returns>parameters.</returns>
        protected abstract Task<Dictionary<string, Dictionary<string, object>>> GetVMParametersAsync(
          VirtualMachineProviderCreateInput input,
          string virtualMachineName,
          IDictionary<string, string> resourceTags,
          string storageAccountName,
          string storageAccountAccessKey,
          string vmInitScriptFileUri,
          string userName,
          IDictionary<string, object> initScriptParametersBlob);

        /// <summary>
        /// Executes pre create task.
        /// </summary>
        /// <param name="input">Vm input.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Task.</returns>
        protected abstract Task PreCreateTaskAsync(
            VirtualMachineProviderCreateInput input,
            IDiagnosticsLogger logger);

        private string GetVmTemplate(string templateName)
        {
            var namespaceString = typeof(VirtualMachineDeploymentManager).Namespace;
            var fullyQualifiedResourceName = $"{namespaceString}.Templates.Windows.{templateName}";
            return CommonUtils.GetEmbeddedResourceContent(fullyQualifiedResourceName);
        }

        private string GetVirtualMachineName()
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