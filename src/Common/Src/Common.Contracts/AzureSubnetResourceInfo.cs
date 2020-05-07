// <copyright file="AzureSubnetResourceInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Azure Subnet Resource Info.
    /// </summary>
    public class AzureSubnetResourceInfo
    {
        /// <summary>
        /// Gets or sets vNet name.
        /// </summary>
        public string VnetName { get; set; }

        /// <summary>
        /// Gets or sets subnet name.
        /// </summary>
        public string SubnetName { get; set; }

        /// <summary>
        /// Gets or sets vNet subscription.
        /// </summary>
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets vNet resource group.
        /// </summary>
        public string ResourceGroup { get; set; }
    }
}
