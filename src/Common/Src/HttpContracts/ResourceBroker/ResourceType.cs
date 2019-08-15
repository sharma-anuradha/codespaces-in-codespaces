// <copyright file="ResourceType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// The back-end resource type.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// Compute
        /// </summary>
        ComputeVM = 1,

        /// <summary>
        /// Storage
        /// </summary>
        StorageFileShare = 2,
    }
}
