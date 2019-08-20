// <copyright file="UsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class UsageDetail
    {
        /// <summary>
        /// Mapping from environment IDs to environment usage details.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "environments")]
        public IDictionary<string, EnvironmentUsageDetail> Environments { get; set; }

        /// <summary>
        /// Mapping from user IDs to user usage details.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "users")]
        public IDictionary<string, UserUsageDetail> Users { get; set; }
    }
}
