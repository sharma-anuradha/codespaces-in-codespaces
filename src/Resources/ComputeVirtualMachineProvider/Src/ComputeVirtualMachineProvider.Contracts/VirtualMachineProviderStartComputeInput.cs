// <copyright file="VirtualMachineProviderStartComputeInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts
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
        /// <param name="shareConnectionInfo">The file share connection info. This is optional.</param>
        /// <param name="inputParams">The VM params.</param>
        /// <param name="userSecrets">User secrets applicable to the environment.</param>
        /// <param name="computeOS">The ComputeOS.</param>
        /// <param name="location">Azure VM location.</param>
        /// <param name="skuName">The Azure SKU name (vm size) of the compute resource.</param>
        /// <param name="continuationToken">The continuation token.</param>
        public VirtualMachineProviderStartComputeInput(
            AzureResourceInfo azureResourceInfo,
            ShareConnectionInfo shareConnectionInfo,
            IDictionary<string, string> inputParams,
            IEnumerable<UserSecretData> userSecrets,
            string devcontainer,
            ComputeOS computeOS,
            AzureLocation location,
            string skuName,
            string continuationToken)
            : base(continuationToken)
        {
            AzureResourceInfo = Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            FileShareConnection = shareConnectionInfo;
            VmInputParams = Requires.NotNull(inputParams, nameof(inputParams));
            UserSecrets = userSecrets;
            DevContainer = devcontainer;
            ComputeOS = computeOS;
            Location = location;
            SkuName = Requires.NotNull(skuName, nameof(skuName));
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
        /// Gets or sets the user secrets.
        /// </summary>
        public IEnumerable<UserSecretData> UserSecrets { get; set; }

        /// <summary>
        /// Gets or sets the devcontainer json.
        /// </summary>
        public string DevContainer { get; set; }

        /// <summary>
        /// Gets or sets the ComputeOS.
        /// </summary>
        public ComputeOS ComputeOS { get; set; }

        /// <summary>
        /// Gets or sets vm location.
        /// </summary>
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the Azure SKU name of the compute resource.
        /// </summary>
        public string SkuName { get; set; }
    }
}
