// <copyright file="CreateLinuxVirtualMachineWithNicStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies
{
    /// <summary>
    /// CreateLinuxVirtualMachineWithNicStrategy.
    /// </summary>
    public class CreateLinuxVirtualMachineWithNicStrategy : LinuxVirtualMachineStrategyBase
    {
        private const string TemplateName = "template_vm_with_custom_nic.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateLinuxVirtualMachineWithNicStrategy"/> class.
        /// </summary>
        /// <param name="clientFactory">azure client factory.</param>
        /// <param name="queueProvider">queue provider.</param>
        public CreateLinuxVirtualMachineWithNicStrategy(
            IAzureClientFPAFactory clientFactory,
            IQueueProvider queueProvider,
            IConfigurationReader configurationReader)
            : base(clientFactory, queueProvider, configurationReader, TemplateName)
        {
        }

        /// <inheritdoc/>
        public override bool Accepts(VirtualMachineProviderCreateInput input)
        {
            return input.ComputeOS == ComputeOS.Linux
                && input.CustomComponents != default
                && input.CustomComponents.Count == 2
                && input.CustomComponents.Count(x => x.ComponentType == ResourceType.NetworkInterface) == 1
                && input.CustomComponents.Count(x => x.ComponentType == ResourceType.InputQueue) == 1;
        }

        /// <inheritdoc/>
        protected override Dictionary<string, Dictionary<string, object>> GetVMParameters(
            VirtualMachineProviderCreateInput input,
            string virtualMachineName,
            IDictionary<string, string> resourceTags,
            string vmInitScript,
            OSDisk osDisk)
        {
            var networkInterface = input.CustomComponents.Where(c => c.ComponentType == ResourceType.NetworkInterface).Single();
            var imageReference = new ImageReferenceInner(input.AzureVirtualMachineImage);

            return new Dictionary<string, Dictionary<string, object>>()
                {
                    { "adminUserName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, AdminUserName } } },
                    { "adminPublicKeyPath", new Dictionary<string, object>() { { VirtualMachineConstants.Key, PublicKeyPath } } },
                    { "adminPublicKey", new Dictionary<string, object>() { { VirtualMachineConstants.Key, VmPublicSshKey } } },
                    { "location", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.AzureVmLocation.ToString() } } },
                    { "virtualMachineName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, virtualMachineName } } },
                    { "virtualMachineSize", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.AzureSkuName } } },
                    { "vmSetupScript", new Dictionary<string, object>() { { VirtualMachineConstants.Key, vmInitScript } } },
                    { "resourceTags", new Dictionary<string, object>() { { VirtualMachineConstants.Key, resourceTags } } },
                    { "networkInterfaceName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, networkInterface.AzureResourceInfo.Name } } },
                    { "networkInterfaceSub", new Dictionary<string, object>() { { VirtualMachineConstants.Key, networkInterface.AzureResourceInfo.SubscriptionId } } },
                    { "networkInterfaceRG", new Dictionary<string, object>() { { VirtualMachineConstants.Key, networkInterface.AzureResourceInfo.ResourceGroup } } },
                    { "imageReference", new Dictionary<string, object>() { { VirtualMachineConstants.Key, imageReference } } },
                    { "osDisk", new Dictionary<string, object>() { { VirtualMachineConstants.Key, osDisk } } },
                };
        }
    }
}