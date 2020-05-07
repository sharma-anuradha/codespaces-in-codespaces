// <copyright file="CreateWindowsVirtualMachineWithNicAndOSDiskStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies
{
    /// <summary>
    /// Creates windows vm with provided network interface and os disk.
    /// </summary>
    public class CreateWindowsVirtualMachineWithNicAndOSDiskStrategy : WindowsVirtualMachineStrategyBase
    {
        private const string TemplateName = "template_vm.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateWindowsVirtualMachineWithNicAndOSDiskStrategy"/> class.
        /// </summary>
        /// <param name="clientFactory">client factory.</param>
        /// <param name="queueProvider">queue provider.</param>
        /// <param name="templateName">vm template name.</param>
        /// <param name="controlPlaneAzureResourceAccessor">control plane azure resource accessor.</param>
        public CreateWindowsVirtualMachineWithNicAndOSDiskStrategy(
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
                && CreateWithNic(input)
                && CreateWithOSDisk(input);
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
            throw new System.NotImplementedException();
        }
    }
}