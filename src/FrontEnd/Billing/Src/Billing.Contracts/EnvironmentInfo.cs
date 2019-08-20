// <copyright file="EnvironmentInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class EnvironmentInfo
    {
        /// <summary>
        /// Environment ID.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// User-assigned name of the environment.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The Cloud Environments (VSLS) profile ID of the user of the environment
        /// (not necessarily the account owner).
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "userId")]
        public string UserId { get; set; }
    }
}
