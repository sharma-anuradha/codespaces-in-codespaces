// <copyright file="VirtualMachineProviderDeleteInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// Provides input to delete virtual machine.
    /// </summary>
    public class VirtualMachineProviderDeleteInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets he azure resource to be deleted.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the virtual machine location.
        /// </summary>
        public AzureLocation AzureVmLocation { get; set; }
    }
}
