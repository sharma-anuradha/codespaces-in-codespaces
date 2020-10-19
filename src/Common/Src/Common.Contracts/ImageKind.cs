// <copyright file="ImageKind.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Indicates the kind of vm image name.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ImageKind
    {
        /// <summary>
        /// The image name spcifies a conanical VM image name.
        /// </summary>
        Canonical = 0,

        /// <summary>
        /// The image name specifies a custom VM image resource.
        /// </summary>
        Custom = 1,

        /// <summary>
        /// The image name specifies a custom Ubuntu VM image resource.
        /// </summary>
        Ubuntu = 2,

        /// <summary>
        /// The image name specifies a custom Ubuntu GPU VM image resource.
        /// </summary>
        UbuntuGPU = 3,
    }
}
