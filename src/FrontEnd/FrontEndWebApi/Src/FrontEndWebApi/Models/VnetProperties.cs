// <copyright file="VnetProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Plan properties for VNet Injection.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class VnetProperties
    {
        /// <summary>
        /// Gets or sets the subnet resource id to use for creating environments in this plan.
        /// </summary>
        public string SubnetId { get; set; }
    }
}
