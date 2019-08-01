// <copyright file="VirtualMachineProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class VirtualMachineProviderAssignResult : BaseContinuationResult
    {
        /// <summary>
        /// 
        /// Example: '/subscriptions/2fa47206-c4b5-40ff-a5e6-9160f9ee000c/storage/<uid>'
        /// </summary>V
        public string ResourceId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string TrackingId { get; set; }
    }
}
