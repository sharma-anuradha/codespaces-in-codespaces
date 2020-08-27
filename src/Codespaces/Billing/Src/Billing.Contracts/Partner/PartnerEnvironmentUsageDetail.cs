// <copyright file="PartnerEnvironmentUsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
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
        /// Initializes a new instance of the <see cref="PartnerEnvironmentUsageDetail"/> class.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="detail">The environment detail.</param>
        public PartnerEnvironmentUsageDetail(EnvironmentUsage detail)
        {
            this.Id = detail.Id;
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
        [JsonProperty(Required = Required.Default, PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets resource usage by Compute and Storage usage rolled up by active/inactive usage.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "resourceUsage")]
        public ResourceUsageDetail ResourceUsage { get; set; }

        /// <summary>
        /// Gets the total compute time.
        /// </summary>
        /// <returns>Returns total compute time.</returns>
        [JsonIgnore]
        public double TotalComputeTime => this?.ResourceUsage?.Compute?.Sum(x => x.Usage) ?? 0;

        /// <summary>
        /// Gets the total storage time.
        /// </summary>
        /// <returns>Returns total storage time.</returns>
        [JsonIgnore]
        public double TotalStorageTime => this?.ResourceUsage?.Storage?.Sum(x => x.Usage) ?? 0;

        /// <summary>
        /// Returns if the detail has any actual data.
        /// </summary>
        /// <returns>Returns true or false.</returns>
        public bool IsEmpty() => TotalComputeTime == 0 && TotalStorageTime == 0;
    }
}
