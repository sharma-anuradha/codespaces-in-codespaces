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
        /// A standard tier SKU, 4 vCPU, FSv2 compute, 64GB Storage.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// A premium tier SKU, 8 vCPU, FSv2 compute, 64GB Storage.
        /// </summary>
        Premium = 1,

        /// <summary>
        /// A premium tier SKU, 4 vCPU, DSv3 compute.
        /// </summary>
        StandardDSv3 = 2,

        /// <summary>
        /// A premium tier SKU, 8 vCPU, DSv3 compute.
        /// </summary>
        PremiumDSv3 = 3,

        /// <summary>
        /// A basic tier SKU, 2 vCPU, FSv2 compute, 64GB Storage.
        /// </summary>
        Basic = 4,

        /// <summary>
        /// A basic tier SKU, 2 vCPU, FSv2 compute, 32GB Storage.
        /// </summary>
        Basic32gb = 5,

        /// <summary>
        /// A standard tier SKU, 4 vCPU, FSv2 compute, 32GB Storage.
        /// </summary>
        Standard32gb = 6,

        /// <summary>
        /// A premium tier SKU, 8 vCPU, FSv2 compute, 32GB Storage.
        /// </summary>
        Premium32gb = 7,

        /// <summary>
        /// A premium tier SKU, 8 vCPU, FSv2 compute, 1TB storage
        /// </summary>
        UltimatePremium = 8,
    }
}
