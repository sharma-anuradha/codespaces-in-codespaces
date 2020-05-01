﻿// <copyright file="DiskProviderAcquireOSDiskResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models
{
    /// <summary>
    /// Operating System Disk get result.
    /// </summary>
    public class DiskProviderAcquireOSDiskResult
    {
        /// <summary>
        /// Gets or sets the azure resource info for acquired OS disk.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }
    }
}
