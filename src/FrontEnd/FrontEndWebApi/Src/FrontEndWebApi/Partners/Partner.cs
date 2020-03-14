// <copyright file="Partner.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Partners
{
    /// <summary>
    /// Set of known external partners which have some custom integrations.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Partner
    {
        /// <summary>
        /// GitHub partner
        /// </summary>
        GitHub,
    }
}
