// <copyright file="VirtualMachineProviderUpdateTagsInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts
{
    /// <summary>
    ///  Virtual Machine Provider Update Tags Input.
    /// </summary>
    public class VirtualMachineProviderUpdateTagsInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the Virtual machine info.
        /// </summary>
        public AzureResourceInfo VirtualMachineResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the resource tags for vm.
        /// </summary>
        public IDictionary<string, string> AdditionalComputeResourceTags { get; set; }

        /// <summary>
        /// Gets or sets the dependent resources.
        /// </summary>
        public IList<ResourceComponent> CustomComponents { get; set; } = new List<ResourceComponent>();
    }
}