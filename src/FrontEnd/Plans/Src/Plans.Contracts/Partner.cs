// <copyright file="Partner.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts
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
