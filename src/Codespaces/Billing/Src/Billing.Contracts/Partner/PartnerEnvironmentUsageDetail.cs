// <copyright file="PartnerEnvironmentUsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Details of the environment usage.
    /// </summary>
    public class PartnerEnvironmentUsageDetail
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartnerEnvironmentUsageDetail"/> class.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="detail">The environment detail.</param>
        public PartnerEnvironmentUsageDetail(string id, EnvironmentUsageDetail detail)
        {
            this.Id = id;
            this.Name = detail.Name;
            this.ResourceUsage = detail.ResourceUsage;
        }

        /// <summary>
        /// Gets or sets the environment id.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the environment name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets resource usage by Compute and Storage usage rolled up by active/inactive usage.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "resourceUsage")]
        public ResourceUsageDetail ResourceUsage { get; set; }
    }
}
