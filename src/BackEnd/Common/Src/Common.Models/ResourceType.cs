// <copyright file="ResourceType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{

    /// <summary>
    /// A Cloud Environment back-end resource type.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// The Cloud Environment back-end compute vm resource type.
        /// </summary>
        ComputeVM = 1,

        /// <summary>
        /// The Cloud Environment back-end storage account resource type.
        /// </summary>
        StorageFileShare = 2,
    }
}
