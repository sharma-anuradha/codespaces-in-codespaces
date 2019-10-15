// <copyright file="VmImageKind.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Indicates the kind of vm image name.
    /// </summary>
    public enum VmImageKind
    {
        /// <summary>
        /// The image name spcifies a conanical VM image name.
        /// </summary>
        Canonical = 0,

        /// <summary>
        /// The image name specifies a custom VM image resource.
        /// </summary>
        Custom = 1,
    }
}
