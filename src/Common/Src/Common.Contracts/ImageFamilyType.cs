// <copyright file="ImageFamilyType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// An image family type that is used to differentiate different image families.
    /// </summary>
    public enum ImageFamilyType
    {
        /// <summary>
        /// Represents compute/VM images
        /// </summary>
        Compute = 0,

        /// <summary>
        /// Represents VM agent images
        /// </summary>
        VmAgent = 1,

        /// <summary>
        /// Represents storage images (e.g. file share seeds)
        /// </summary>
        Storage = 2,
    }
}
