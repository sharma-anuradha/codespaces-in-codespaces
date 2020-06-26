// <copyright file="SecretStore.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts
{
    /// <summary>
    /// Secret store.
    /// </summary>
    public class SecretStore : TaggedEntity
    {
        /// <summary>
        /// Gets or sets the secret scope.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "scope")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SecretScope Scope { get; set; }

        /// <summary>
        /// Gets or sets the plan id.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "planId")]
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets the owner id.
        /// This will be the userId if the scope is user, planId if the scope is plan.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the backend resource info.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "secretResource")]
        public ResourceAllocationRecord SecretResource { get; set; }
    }
}