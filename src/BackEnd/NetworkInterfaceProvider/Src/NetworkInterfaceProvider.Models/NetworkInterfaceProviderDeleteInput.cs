// <copyright file="NetworkInterfaceProviderDeleteInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Models
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