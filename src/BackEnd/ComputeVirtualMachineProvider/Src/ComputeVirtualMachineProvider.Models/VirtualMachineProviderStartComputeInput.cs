// <copyright file="VirtualMachineProviderStartComputeInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    ///
    /// </summary>
    public class VirtualMachineProviderStartComputeInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineProviderStartComputeInput"/> class.
        /// </summary>
        public VirtualMachineProviderStartComputeInput()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineProviderStartComputeInput"/> class.
        /// </summary>
        /// <param name="computeResourceId">The compute resource broker id.</param>
        /// <param name="computeAzureResourceInfo">The compute azure resource info.</param>
        /// <param name="shareConnectionInfo">The file share connection info.</param>
        /// <param name="inputParams">The VM params.</param>
        public VirtualMachineProviderStartComputeInput(
            Guid computeResourceId,
            AzureResourceInfo computeAzureResourceInfo,
            ShareConnectionInfo shareConnectionInfo,
            IDictionary<string, string> inputParams)
        {
            Requires.Argument(computeResourceId != default, nameof(computeResourceId), "required");
            Requires.NotNull(computeAzureResourceInfo, nameof(computeAzureResourceInfo));
            ComputeResourceId = computeResourceId;
            ComputeAzureResourceInfo = computeAzureResourceInfo;
            FileShareConnection = shareConnectionInfo;
            VmInputParams = inputParams;
        }

        /// <summary>
        /// Gets the resource broker compute resource id.
        /// </summary>
        public Guid ComputeResourceId { get; set; }

        /// <summary>
        /// Gets the azure resource info.
        /// </summary>
        public AzureResourceInfo ComputeAzureResourceInfo { get; set; }

        /// <summary>
        /// Gets the file share connection info.
        /// </summary>
        public ShareConnectionInfo FileShareConnection { get; set; }

        /// <summary>
        /// Gets the virtual-machine parameters.
        /// </summary>
        public IDictionary<string, string> VmInputParams { get; set; }
    }
}
