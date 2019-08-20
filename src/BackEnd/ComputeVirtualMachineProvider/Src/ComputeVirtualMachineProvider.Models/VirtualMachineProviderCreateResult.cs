// <copyright file="VirtualMachineProviderCreateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// Result returned for virtual machine creation.
    /// </summary>
    public class VirtualMachineProviderCreateResult : BaseContinuationResult
    {
        /// <summary>
        /// ResourceId for virtual machine.
        /// </summary>
        public ResourceId ResourceId { get; set; }
    }
}
