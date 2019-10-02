// <copyright file="EnvironmentUsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class EnvironmentUsageDetail
    {
        /// <summary>
        /// Environment name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        // TODO: Consider adding other environment details here that might
        // be relevant to understanding the bill, such as the env size.

        /// <summary>
        /// State of the environment at the end of the period.
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
        /// Usage billed to the environment for the billing period for one or more
        /// billing meters, in each meter's units.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usage")]
        public IDictionary<string, double> Usage { get; set; }

        /// <summary>
        /// The Cloud Environments (VSLS) profile ID of the user of the environment.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the SKU of the cloud environment.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "sku")]
        public Sku Sku { get; set; }

    }
}
