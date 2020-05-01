// <copyright file="ResourceAllocationResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
        public ResourceAllocation Compute
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the storage resource.
        /// </summary>
        public ResourceAllocation Storage
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the OS Disk resource.
        /// </summary>
        public ResourceAllocation OSDisk
        {
            get;
            set;
        }
    }
}
