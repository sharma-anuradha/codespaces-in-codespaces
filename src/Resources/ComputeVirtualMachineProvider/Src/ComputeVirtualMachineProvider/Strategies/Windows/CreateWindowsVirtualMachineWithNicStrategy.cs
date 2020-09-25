// <copyright file="CreateWindowsVirtualMachineWithNicStrategy.cs" company="Microsoft">
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
    /// CreateLinuxVirtualMachineWithNicStrategy.
    /// </summary>
    public class CreateWindowsVirtualMachineWithNicStrategy : WindowsVirtualMachineStrategyBase
    {
        private const string TemplateName = "template_vm_with_custom_nic.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateWindowsVirtualMachineWithNicStrategy"/> class.
        /// </summary>
        /// <param name="clientFactory">client factory.</param>
        /// <param name="queueProvider">queue provider.</param>
        /// <param name="templateName">vm template name.</param>
        /// <param name="initScriptUrlGenerator">Init script url generator.</param>
        public CreateWindowsVirtualMachineWithNicStrategy(
            IAzureClientFPAFactory clientFactory,
            IQueueProvider queueProvider,
            IInitScriptUrlGenerator initScriptUrlGenerator)
            : base(clientFactory, queueProvider, TemplateName, initScriptUrlGenerator)
        {
        }

        /// <inheritdoc/>
        public override bool Accepts(VirtualMachineProviderCreateInput input)
        {
            return input.ComputeOS == ComputeOS.Windows
                && CreateWithNic(input)
                && !CreateWithOSDisk(input);
        }

        /// <inheritdoc/>
        protected override Task<Dictionary<string, Dictionary<string, object>>> GetVMParametersAsync(
             VirtualMachineProviderCreateInput input,
             string virtualMachineName,
             IDictionary<string, string> resourceTags,
             string vmInitScriptFileUri,
             string userName,
             IDictionary<string, object> initScriptParametersBlob,
             IDiagnosticsLogger logger)
        {
            var networkInterface = input.CustomComponents.Where(c => c.ComponentType == ResourceType.NetworkInterface).Single();
            initScriptParametersBlob["firstBoot"] = "true";
            var b64ParametersBlob = EncodeScriptParameters(initScriptParametersBlob);
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

            return Task.FromResult(new Dictionary<string, Dictionary<string, object>>()
                {
                    { "location", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.AzureVmLocation.ToString() } } },
                    { "virtualMachineRG", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.AzureResourceGroup } } },
                    { "virtualMachineName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, virtualMachineName } } },
                    { "virtualMachineSize", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.AzureSkuName } } },
                    { "adminUserName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, userName } } },
                    { "adminPassword", new Dictionary<string, object>() { { VirtualMachineConstants.Key, Guid.NewGuid() } } },
                    { "resourceTags", new Dictionary<string, object>() { { VirtualMachineConstants.Key, resourceTags } } },
                    { "vmInitScriptFileUri", new Dictionary<string, object>() { { VirtualMachineConstants.Key, vmInitScriptFileUri } } },
                    { PayloadExtensions.VMAgentBlobUrl, new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.VmAgentBlobUrl } } },
                    { "vmInitScriptBase64ParametersBlob", new Dictionary<string, object>() { { VirtualMachineConstants.Key, b64ParametersBlob } } },
                    { "networkInterfaceName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, networkInterface.AzureResourceInfo.Name } } },
                    { "networkInterfaceSub", new Dictionary<string, object>() { { VirtualMachineConstants.Key, networkInterface.AzureResourceInfo.SubscriptionId } } },
                    { "networkInterfaceRG", new Dictionary<string, object>() { { VirtualMachineConstants.Key, networkInterface.AzureResourceInfo.ResourceGroup } } },
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