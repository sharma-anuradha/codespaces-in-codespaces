// <copyright file="VirtualMachineProviderStartComputeInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// Provides input for cloud environment.
    /// </summary>
    public class VirtualMachineProviderStartComputeInput : ContinuationInput
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
        /// <param name="azureResourceInfo">The compute azure resource info.</param>
        /// <param name="shareConnectionInfo">The file share connection info.</param>
        /// <param name="inputParams">The VM params.</param>
        /// <param name="computeOS">The ComputeOS.</param>
        /// <param name="location">Azure VM location.</param>
        /// <param name="continuationToken">The continuation token.</param>
        public VirtualMachineProviderStartComputeInput(
            AzureResourceInfo azureResourceInfo,
            ShareConnectionInfo shareConnectionInfo,
            IDictionary<string, string> inputParams,
            ComputeOS computeOS,
            AzureLocation location,
            string continuationToken)
            : base(continuationToken)
        {
            AzureResourceInfo = azureResourceInfo;
            FileShareConnection = shareConnectionInfo;
            VmInputParams = inputParams;
            ComputeOS = computeOS;
            Location = location;
        }

        /// <summary>
        /// Gets or sets the azure resource info object instance.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets file share connection object instance.
        /// </summary>
        public ShareConnectionInfo FileShareConnection { get; set; }

        /// <summary>
        /// Gets or sets the input parameters to start the environment.
        /// </summary>
        public IDictionary<string, string> VmInputParams { get; set; }

        /// <summary>
        /// Gets or sets the ComputeOS.
        /// </summary>
        public ComputeOS ComputeOS { get; set; }

        /// <summary>
        /// Gets or sets vm location.
        /// </summary>
        public AzureLocation Location { get; set; }
    }
}
