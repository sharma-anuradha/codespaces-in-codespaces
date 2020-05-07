// <copyright file="CreateWindowsVirtualMachineWithOSDiskStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies
{
    /// <summary>
    /// Creates Windows Virtual Machine with provided os disk.
    /// </summary>
    public class CreateWindowsVirtualMachineWithOSDiskStrategy : WindowsVirtualMachineStrategyBase
    {
        private const string TemplateName = "template_vm_with_custom_disk.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateWindowsVirtualMachineWithOSDiskStrategy"/> class.
        /// </summary>
        /// <param name="clientFactory">client factory.</param>
        /// <param name="queueProvider">queue provider.</param>
        /// <param name="templateName">vm template name.</param>
        /// <param name="controlPlaneAzureResourceAccessor">control plane azure resource accessor.</param>
        public CreateWindowsVirtualMachineWithOSDiskStrategy(
            IAzureClientFactory clientFactory,
            IQueueProvider queueProvider,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
            : base(clientFactory, queueProvider, TemplateName, controlPlaneAzureResourceAccessor)
        {
        }

        /// <inheritdoc/>
        public override bool Accepts(VirtualMachineProviderCreateInput input)
        {
            return input.ComputeOS == ComputeOS.Windows
                && !CreateWithNic(input)
                && CreateWithOSDisk(input);
        }

        /// <inheritdoc/>
        protected override async Task<Dictionary<string, Dictionary<string, object>>> GetVMParametersAsync(
            VirtualMachineProviderCreateInput input,
            string virtualMachineName,
            IDictionary<string, string> resourceTags,
            string storageAccountName,
            string storageAccountAccessKey,
            string vmInitScriptFileUri,
            string userName,
            IDictionary<string, object> initScriptParametersBlob)
        {
            var createWithOSDisk = input.CustomComponents.Any(x =>
                                           x.ComponentType == ResourceType.OSDisk &&
                                           !string.IsNullOrEmpty(x.AzureResourceInfo?.Name));

            if (!createWithOSDisk)
            {
                initScriptParametersBlob["firstBoot"] = "true";
            }

            var b64ParametersBlob = EncodeScriptParameters(initScriptParametersBlob);
            var osDiskInfo = input.CustomComponents.Single(x => x.ComponentType == ResourceType.OSDisk).AzureResourceInfo;

            // TODO:: May move to disk provider.
            var disk = await ValidateOSDisk(input, osDiskInfo);
            var storageProfile = new StorageProfile()
            {
                OsDisk = new OSDisk(
                     DiskCreateOptionTypes.Attach,
                     OperatingSystemTypes.Windows,
                     managedDisk: new ManagedDiskParametersInner(
                         storageAccountType: StorageAccountTypes.PremiumLRS,
                         id: disk.Id)),
            };

            return new Dictionary<string, Dictionary<string, object>>()
                {
                    { "location", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.AzureVmLocation.ToString() } } },
                    { "virtualMachineRG", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.AzureResourceGroup } } },
                    { "virtualMachineName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, virtualMachineName } } },
                    { "virtualMachineSize", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.AzureSkuName } } },
                    { "networkSecurityGroupName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, VirtualMachineResourceNames.GetNetworkSecurityGroupName(virtualMachineName) } } },
                    { "networkInterfaceName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, VirtualMachineResourceNames.GetNetworkInterfaceName(virtualMachineName) } } },
                    { "virtualNetworkName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, VirtualMachineResourceNames.GetVirtualNetworkName(virtualMachineName) } } },
                    { "adminUserName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, userName } } },
                    { "adminPassword", new Dictionary<string, object>() { { VirtualMachineConstants.Key, Guid.NewGuid() } } },
                    { "resourceTags", new Dictionary<string, object>() { { VirtualMachineConstants.Key, resourceTags } } },
                    { "vmInitScriptFileUri", new Dictionary<string, object>() { { VirtualMachineConstants.Key, vmInitScriptFileUri } } },
                    { "vmAgentBlobUrl", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.VmAgentBlobUrl } } },
                    { "vmInitScriptStorageAccountName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, storageAccountName } } },
                    { "vmInitScriptStorageAccountKey", new Dictionary<string, object>() { { VirtualMachineConstants.Key, storageAccountAccessKey } } },
                    { "vmInitScriptBase64ParametersBlob", new Dictionary<string, object>() { { VirtualMachineConstants.Key, b64ParametersBlob } } },
                    { "storageProfileDetails", new Dictionary<string, object>() { { VirtualMachineConstants.Key, storageProfile } } },
                };
        }

        private async Task<IDisk> ValidateOSDisk(VirtualMachineProviderCreateInput input, AzureResourceInfo osDiskInfo)
        {
            var azure = await ClientFactory.GetAzureClientAsync(input.AzureSubscription);
            var disk = await azure.Disks.GetByResourceGroupAsync(osDiskInfo.ResourceGroup, osDiskInfo.Name);

            if (disk.IsAttachedToVirtualMachine)
            {
                throw new InvalidOperationException("Should not try to use a disk which is already attached.");
            }

            return disk;
        }
    }
}