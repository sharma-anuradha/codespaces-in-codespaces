// <copyright file="EnvironmentUsage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Details of the environment usage.
    /// </summary>
    public class EnvironmentUsage
    {
        /// <summary>
        /// Gets or sets the environment name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets state of the environment at the end of the period.
        /// </summary>
        /// <seealso cref="EnvironmentStates" />
        /// <remarks>
        /// This can be used by the billing service to efficiently find the current state of
        /// active environments (those which have current or recent usage), by starting with the
        /// most recent billing summary event and applying any state change events after that.
        /// Or if there was no usage detail for an environment for the previous period summary
        /// then the environment must have been inactive during that entire period.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "endState")]
        public string EndState { get; set; }

        /// <summary>
        /// Gets or sets usage billed to the environment for the billing period for one or more
        /// billing meters, in each meter's units.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usage")]
        public IDictionary<string, double> Usage { get; set; }

        /// <summary>
        /// Gets or sets resource usage by Compute and Storage usage rolled up by active/inactive usage.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "resourceUsage")]
        public ResourceUsageDetail ResourceUsage { get; set; }

        /// <summary>
        /// Gets or sets the SKU of the cloud environment.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "sku")]
        public Sku Sku { get; set; }
    }
}
