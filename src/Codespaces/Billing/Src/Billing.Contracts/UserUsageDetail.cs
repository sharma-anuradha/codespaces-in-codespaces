// <copyright file="UserUsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class UserUsageDetail
    {
        // TODO: Consider adding a user name or email property here
        // to avoid having to do lots of lookups of the user ID.

        /// <summary>
        /// Total usage billed to the user for the billing period for one or more
        /// billing meters, in each meter's units.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usage")]
        public IDictionary<string, double> Usage { get; set; }
    }
}
