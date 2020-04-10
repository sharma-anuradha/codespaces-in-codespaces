// <copyright file="AzureGeography.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// Indicates a public Azure Geography.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AzureGeography
    {
        /// <summary>
        /// Asia Pacific.
        /// </summary>
        AsiaPacific,

        /// <summary>
        /// Brazil.
        /// </summary>
        Brazil,

        /// <summary>
        /// Canada.
        /// </summary>
        Canada,

        /// <summary>
        /// China.
        /// </summary>
        China,

        /// <summary>
        /// Europe (other, if country-specific geo not available)
        /// </summary>
        Europe,

        /// <summary>
        /// France.
        /// </summary>
        France,

        /// <summary>
        /// Germany.
        /// </summary>
        Germany,

        /// <summary>
        /// India.
        /// </summary>
        India,

        /// <summary>
        /// Japan.
        /// </summary>
        Japan,

        /// <summary>
        /// Korea.
        /// </summary>
        Korea,

        /// <summary>
        /// Mexico.
        /// </summary>
        Mexico,

        /// <summary>
        /// Norway.
        /// </summary>
        Norway,

        /// <summary>
        /// Poland.
        /// </summary>
        Poland,

        /// <summary>
        /// South Africa.
        /// </summary>
        SouthAfrica,

        /// <summary>
        /// Spain.
        /// </summary>
        Spain,

        /// <summary>
        /// Sweden.
        /// </summary>
        Sweden,

        /// <summary>
        /// Swizerland.
        /// </summary>
        Switzerland,

        /// <summary>
        /// Taiwan.
        /// </summary>
        Taiwan,

        /// <summary>
        /// United Arab Emerates.
        /// </summary>
        UAE,

        /// <summary>
        /// United Kingdom.
        /// </summary>
        UnitedKingdom,

        /// <summary>
        /// United States.
        /// </summary>
        UnitedStates,

        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,
    }
}
