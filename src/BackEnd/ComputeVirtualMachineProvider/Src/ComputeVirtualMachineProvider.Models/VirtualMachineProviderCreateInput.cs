// <copyright file="VirtualMachineProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Security.Authentication.ExtendedProtection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// Provides input to create virtual machine.
    /// </summary>
    public class VirtualMachineProviderCreateInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the vmtoken.
        /// </summary>
        public string VMToken { get; set; }

        /// <summary>
        /// Gets or sets virtual machine subscription.
        /// </summary>
        public Guid AzureSubscription { get; set; }

        /// <summary>
        /// Gets or sets virtual machine  location.
        /// </summary>
        public AzureLocation AzureVmLocation { get; set; } // 'westus2'

        /// <summary>
        /// Gets or sets virtual machine resource group.
        /// </summary>
        public string AzureResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets virtual machine sku.
        /// </summary>
        public string AzureSkuName { get; set; }

        /// <summary>
        /// Gets or sets virtual machine image.
        /// </summary>
        public string AzureVirtualMachineImage { get; set; }
    }
}