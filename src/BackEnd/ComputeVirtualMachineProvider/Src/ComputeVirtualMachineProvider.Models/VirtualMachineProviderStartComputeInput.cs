// <copyright file="VirtualMachineProviderAllocateInput.cs" company="Microsoft">
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
    public class VirtualMachineProviderStartComputeInput
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineProviderStartComputeInput"/> class.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info object.</param>
        /// <param name="shareConnectionInfo">File share connection details.</param>
        /// <param name="inputParams">Environment creation input.</param>
        public VirtualMachineProviderStartComputeInput(
            AzureResourceInfo azureResourceInfo,
            ShareConnectionInfo shareConnectionInfo,
            IDictionary<string, string> inputParams)
        {
            AzureResourceInfo = azureResourceInfo;
            FileShareConnection = shareConnectionInfo;
            VmInputParams = inputParams;
        }

        /// <summary>
        /// Gets the azure resource info object instance.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; }

        /// <summary>
        /// Gets file share connection object instance.
        /// </summary>
        public ShareConnectionInfo FileShareConnection { get; }

        /// <summary>
        /// Gets the input parameters to start the environment.
        /// </summary>
        public IDictionary<string, string> VmInputParams { get; }
    }
}
