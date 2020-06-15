// <copyright file="VirtualMachineProviderShutdownInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// Provides input to cleanup virtual machine.
    /// </summary>
    public class VirtualMachineProviderShutdownInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets he azure resource to be cleaned.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the virtual machine location.
        /// </summary>
        public AzureLocation AzureVmLocation { get; set; }

        /// <summary>
        /// Gets or sets the ComputeOS.
        /// </summary>
        public ComputeOS ComputeOS { get; set; }

        /// <summary>
        /// Gets or sets the environment id.
        /// </summary>
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to preserve the OS disk.
        /// </summary>
        public bool PreserveOSDisk { get; set; }
    }
}
