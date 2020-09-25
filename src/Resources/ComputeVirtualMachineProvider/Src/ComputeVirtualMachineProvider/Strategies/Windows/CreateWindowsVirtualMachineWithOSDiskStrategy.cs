// <copyright file="CreateWindowsVirtualMachineWithOSDiskStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;

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
        /// <param name="initScriptUrlGenerator">Init script url generator.</param>
        public CreateWindowsVirtualMachineWithOSDiskStrategy(
            IAzureClientFactory clientFactory,
            IQueueProvider queueProvider,
            IInitScriptUrlGenerator initScriptUrlGenerator)
            : base(clientFactory, queueProvider, TemplateName, initScriptUrlGenerator)
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
            string vmInitScriptFileUri,
            string userName,
            IDictionary<string, object> initScriptParametersBlob,
            IDiagnosticsLogger logger)
        {
            var createWithOSDisk = input.CustomComponents.Any(x =>
                                           x.ComponentType == ResourceType.OSDisk &&
                                           !string.IsNullOrEmpty(x.AzureResourceInfo?.Name));

            if (createWithOSDisk)
            {
                if (input.Options == default)
                {
                    input.Options = new VirtualMachineResumeOptions()
                    {
                        HardBoot = false,
                    };
                }
            }

            var runCustomScriptExtension = "Yes";
            if (input.Options is VirtualMachineResumeOptions resumeOptions)
            {
                runCustomScriptExtension = resumeOptions.HardBoot ? "Yes" : "No";
            }

            var b64ParametersBlob = EncodeScriptParameters(initScriptParametersBlob);
            var osDiskInfo = input.CustomComponents.Single(x => x.ComponentType == ResourceType.OSDisk).AzureResourceInfo;

            // TODO:: May move to disk provider.
            var disk = await ValidateOSDisk(input, osDiskInfo, logger);
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
                    { "runCustomScriptExtension", new Dictionary<string, object>() { { VirtualMachineConstants.Key, runCustomScriptExtension } } },
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
                    { PayloadExtensions.VMAgentBlobUrl, new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.VmAgentBlobUrl } } },
                    { "vmInitScriptBase64ParametersBlob", new Dictionary<string, object>() { { VirtualMachineConstants.Key, b64ParametersBlob } } },
                    { "storageProfileDetails", new Dictionary<string, object>() { { VirtualMachineConstants.Key, storageProfile } } },
                };
        }
    }
}