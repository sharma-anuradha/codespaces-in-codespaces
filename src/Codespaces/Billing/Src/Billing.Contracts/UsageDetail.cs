// <copyright file="UsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Usage detail.
    /// </summary>
    public class UsageDetail
    {
        /// <summary>
        /// Gets or sets mapping from environment IDs to environment usage details.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "environments")]
        public IDictionary<string, EnvironmentUsageDetail> Environments { get; set; }

        /// <summary>
        /// Gets or sets mapping from user IDs to user usage details.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "users")]
        public IDictionary<string, UserUsageDetail> Users { get; set; }
    }
}
