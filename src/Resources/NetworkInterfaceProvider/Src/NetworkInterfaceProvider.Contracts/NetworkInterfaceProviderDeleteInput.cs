// <copyright file="NetworkInterfaceProviderDeleteInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts
{
    /// <summary>
    /// Network Interface Provider Delete Input.
    /// </summary>
    public class NetworkInterfaceProviderDeleteInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets network Interface Resource Info.
        /// </summary>
        public AzureResourceInfo ResourceInfo { get; set; }
    }
}