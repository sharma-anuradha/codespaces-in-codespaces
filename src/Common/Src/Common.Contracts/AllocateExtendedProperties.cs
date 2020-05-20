// <copyright file="AllocateExtendedProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Extended properties for allocation.
    /// </summary>
    public class AllocateExtendedProperties
    {
        /// <summary>
        /// Gets or sets the OS disk resource id.
        /// </summary>
        public string OSDiskResourceID { get; set; }

        /// <summary>
        /// Gets or sets the Azure subnet.
        /// </summary>
        public string SubnetResourceId { get; set; }

        /// <summary>
        /// Gets or sets the allocation request id.
        /// </summary>
        public string AllocationRequestID { get; set; }
    }
}
