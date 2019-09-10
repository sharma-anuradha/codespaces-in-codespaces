// <copyright file="VirtualMachineProviderQueueInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// Provides input get virtual machine queue connection details.
    /// </summary>
    public class VirtualMachineProviderQueueInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the virtual machine location.
        /// </summary>
        public AzureLocation AzureVmLocation { get; set; }

        /// <summary>
        /// Gets or sets the virtual machine name.
        /// </summary>
        public string AzureVmName { get; set; }
    }
}