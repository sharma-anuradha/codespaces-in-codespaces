// <copyright file="SkuTier.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// The Environment SKU tier.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SkuTier
    {
        /// <summary>
        /// A standard tier SKU.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// A premium tier SKU.
        /// </summary>
        Premium = 1,
    }
}
