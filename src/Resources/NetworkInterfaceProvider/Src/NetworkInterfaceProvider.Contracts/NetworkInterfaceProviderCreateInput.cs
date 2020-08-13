// <copyright file="NetworkInterfaceProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts
{
    /// <summary>
    /// Network interface provider input.
    /// </summary>
    public class NetworkInterfaceProviderCreateInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the Subnet azure resource id.  If not specified, the Subnet and VNet will be created.
        /// </summary>
        public string SubnetAzureResourceId { get; set; }

        /// <summary>
        /// Gets or sets vnet azure subscription.
        /// </summary>
        public Guid SubnetSubscription { get; set; }

        /// <summary>
        /// Gets or sets location.
        /// </summary>
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets resource group.
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets resource tags.
        /// </summary>
        public IDictionary<string, string> ResourceTags { get; set; }
    }
}