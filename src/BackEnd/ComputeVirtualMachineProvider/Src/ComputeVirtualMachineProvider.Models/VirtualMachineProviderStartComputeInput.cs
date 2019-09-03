// <copyright file="VirtualMachineProviderStartComputeInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
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
        public VirtualMachineProviderStartComputeInput(
            AzureResourceInfo azureResourceInfo,
            ShareConnectionInfo shareConnectionInfo,
            IDictionary<string, string> inputParams,
            string continuationToken)
            : base(continuationToken)
        {
            AzureResourceInfo = azureResourceInfo;
            FileShareConnection = shareConnectionInfo;
            VmInputParams = inputParams;
        }

        /// <summary>
        /// Gets the azure resource info object instance.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets file share connection object instance.
        /// </summary>
        public ShareConnectionInfo FileShareConnection { get; set; }

        /// <summary>
        /// Gets the input parameters to start the environment.
        /// </summary>
        public IDictionary<string, string> VmInputParams { get; set; }
    }
}
