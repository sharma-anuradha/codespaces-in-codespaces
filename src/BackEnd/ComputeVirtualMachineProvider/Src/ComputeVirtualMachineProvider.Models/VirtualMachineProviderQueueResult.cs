// <copyright file="VirtualMachineProviderQueueResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// Provides azure queue sas token and url.
    /// </summary>
    public class VirtualMachineProviderQueueResult : ContinuationResult
    {
        /// <summary>
        /// Gets or sets / Sets virutal machine queue detials.
        /// </summary>
        public QueueConnectionInfo QueueConnectionInfo { get; set; }
    }
}