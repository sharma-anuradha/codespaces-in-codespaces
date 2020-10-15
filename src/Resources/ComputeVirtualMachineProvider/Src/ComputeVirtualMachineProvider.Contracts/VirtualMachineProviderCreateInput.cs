// <copyright file="VirtualMachineProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts
{
    /// <summary>
    /// Provides input to create virtual machine.
    /// </summary>
    public class VirtualMachineProviderCreateInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the Resource Id.
        /// </summary>
        public string ResourceId { get; set; }

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

        /// <summary>
        /// Gets or sets the disk size.
        /// </summary>
        public int? DiskSize { get; set; }

        /// <summary>
        /// Gets or sets resource tags that should be added to the resource.
        /// </summary>
        public IDictionary<string, string> ResourceTags { get; set; }

        /// <summary>
        /// Gets or sets the ComputeOS.
        /// </summary>
        public ComputeOS ComputeOS { get; set; }

        /// <summary>
        /// Gets or sets the blob url used for virtual machine agent.
        /// </summary>
        public string VmAgentBlobUrl { get; set; }

        /// <summary>
        /// Gets or sets the front end service dns host name.
        /// </summary>
        public string FrontDnsHostName { get; set; }

        /// <summary>
        /// Gets or sets the dependent resources.
        /// </summary>
        public IList<ResourceComponent> CustomComponents { get; set; } = new List<ResourceComponent>();

        /// <summary>
        /// Gets or sets the queue connection info.
        /// </summary>
        public QueueConnectionInfo QueueConnectionInfo { get; set; }

        /// <summary>
        /// Gets or sets the options when an environment is resumed.
        /// </summary>
        public VirtualMachineProviderCreateOptions Options { get; set; }
    }
}