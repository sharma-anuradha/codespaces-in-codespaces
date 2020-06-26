// <copyright file="CreateLinuxVirtualMachineBasicStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies
{
    /// <summary>
    /// Create Linux Basic Virtual Machine Strategy.
    /// </summary>
    public class CreateLinuxVirtualMachineBasicStrategy : LinuxVirtualMachineStrategyBase
    {
        private const string TemplateName = "template_vm.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateLinuxVirtualMachineBasicStrategy"/> class.
        /// </summary>
        /// <param name="clientFactory">azure client factory.</param>
        /// <param name="queueProvider">queue provider.</param>
        public CreateLinuxVirtualMachineBasicStrategy(
            IAzureClientFactory clientFactory,
            IQueueProvider queueProvider)
            : base(clientFactory, queueProvider, TemplateName)
        {
        }

        /// <inheritdoc/>
        public override bool Accepts(VirtualMachineProviderCreateInput input)
        {
            return input.ComputeOS == ComputeOS.Linux &&
                (input.CustomComponents == default ||
                input.CustomComponents.Count == 0 ||
                input.CustomComponents.All(x => x.ComponentType == ResourceType.InputQueue));
        }

        /// <inheritdoc/>
        protected override Dictionary<string, Dictionary<string, object>> GetVMParameters(
           VirtualMachineProviderCreateInput input,
           string virtualMachineName,
           IDictionary<string, string> resourceTags,
           string vmInitScript)
        {
            return new Dictionary<string, Dictionary<string, object>>()
                {
                    { "adminUserName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, AdminUserName } } },
                    { "adminPublicKeyPath", new Dictionary<string, object>() { { VirtualMachineConstants.Key, PublicKeyPath } } },
                    { "adminPublicKey", new Dictionary<string, object>() { { VirtualMachineConstants.Key, VmPublicSshKey } } },
                    { "location", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.AzureVmLocation.ToString() } } },
                    { "osDiskName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, VirtualMachineResourceNames.GetOsDiskName(virtualMachineName) } } },
                    { "networkInterfaceName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, VirtualMachineResourceNames.GetNetworkInterfaceName(virtualMachineName) } } },
                    { "virtualMachineName", new Dictionary<string, object>() { { VirtualMachineConstants.Key, virtualMachineName } } },
                    { "virtualMachineSize", new Dictionary<string, object>() { { VirtualMachineConstants.Key, input.AzureSkuName } } },
                    { "vmSetupScript", new Dictionary<string, object>() { { VirtualMachineConstants.Key, vmInitScript } } },
                    { "resourceTags", new Dictionary<string, object>() { { VirtualMachineConstants.Key, resourceTags } } },
                };
        }
    }
}