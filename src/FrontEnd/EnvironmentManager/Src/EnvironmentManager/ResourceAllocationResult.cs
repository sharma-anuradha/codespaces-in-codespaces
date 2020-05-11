// <copyright file="ResourceAllocationResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Resource allocation result.
    /// </summary>
    public class ResourceAllocationResult
    {
        /// <summary>
        /// Gets or sets the compute resource.
        /// </summary>
        public ResourceAllocationRecord Compute
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the storage resource.
        /// </summary>
        public ResourceAllocationRecord Storage
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the OS Disk resource.
        /// </summary>
        public ResourceAllocationRecord OSDisk
        {
            get;
            set;
        }
    }
}
