// <copyright file="CreateWindowsVirtualMachineBasicStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies
{
    /// <summary>
    /// Creates Windows Basic Virtual Machine.
    /// </summary>
    public class CreateWindowsVirtualMachineBasicStrategy : WindowsVirtualMachineStrategyBase
    {
        private const string TemplateName = "template_vm.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateWindowsVirtualMachineBasicStrategy"/> class.
        /// </summary>
        /// <param name="clientFactory">client factory.</param>
        /// <param name="queueProvider">queue provider.</param>
        /// <param name="templateName">vm template name.</param>
        /// <param name="controlPlaneAzureResourceAccessor">control plane azure resource accessor.</param>
        public CreateWindowsVirtualMachineBasicStrategy(
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
                && !CreateWithOSDisk(input);
        }

        /// <inheritdoc/>
        protected override Task<Dictionary<string, Dictionary<string, object>>> GetVMParametersAsync(
            VirtualMachineProviderCreateInput input,
            string virtualMachineName,
            IDictionary<string, string> resourceTags,
            string storageAccountName,
            string storageAccountAccessKey,
            string vmInitScriptFileUri,
            string userName,
            IDictionary<string, object> initScriptParametersBlob)
        {
            var storageProfile = new StorageProfile()
            {
                ImageReference = new ImageReferenceInner(
                            input.AzureVirtualMachineImage),
                OsDisk = new OSDisk(
                            DiskCreateOptionTypes.FromImage,
                            OperatingSystemTypes.Windows,
                            name: VirtualMachineResourceNames.GetOsDiskName(virtualMachineName),
                            managedDisk: new ManagedDiskParametersInner(storageAccountType: StorageAccountTypes.PremiumLRS)),
            };

            initScriptParametersBlob["firstBoot"] = "true";
            var b64ParametersBlob = EncodeScriptParameters(initScriptParametersBlob);

            return Task.FromResult(new Dictionary<string, Dictionary<string, object>>()
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
                });
        }

        /// <inheritdoc/>
        protected override Task PreCreateTaskAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
        {
            // Nothing to do.
            return Task.CompletedTask;
        }
    }
}