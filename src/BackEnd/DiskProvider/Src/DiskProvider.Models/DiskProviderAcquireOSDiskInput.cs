// <copyright file="DiskProviderAcquireOSDiskInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models
{
    /// <summary>
    /// Inputs for getting the operating system disk.
    /// </summary>
    public class DiskProviderAcquireOSDiskInput
    {
        /// <summary>
        /// Gets or sets he azure resource to be deleted.
        /// </summary>
        public AzureResourceInfo VirtualMachineResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the virtual machine location.
        /// </summary>
        public AzureLocation AzureVmLocation { get; set; }

        /// <summary>
        /// Gets or sets the os disk resource tags.
        /// </summary>
        public IDictionary<string, string> OSDiskResourceTags { get; set; }

        /// <summary>
        /// Gets or sets the compute vm resource tags.
        /// </summary>
        public IDictionary<string, string> AdditionalComputeResourceTags { get; set; }
    }
}
