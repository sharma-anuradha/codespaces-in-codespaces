﻿// <copyright file="VirtualMachineProviderDeleteResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class VirtualMachineProviderDeleteResult : BaseContinuationResult
    {
        /// <summary>
        /// 
        /// </summary>
        public string TrackingId { get; set; }
    }
}
