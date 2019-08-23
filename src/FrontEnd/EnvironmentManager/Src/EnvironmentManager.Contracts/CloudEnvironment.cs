// <copyright file="CloudEnvironment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The top-level environment entity.
    /// </summary>
    public class CloudEnvironment : TaggedEntity
    {
        /// <summary>
        /// Gets or sets the environment type.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public CloudEnvironmentType Type { get; set; }

        /// <summary>
        /// Gets or sets the environment friendly name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "friendlyName")]
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the created date and time.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "created")]
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the updated date and time.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "updated")]
        public DateTime Updated { get; set; }

        /// <summary>
        /// Gets or sets the owner id.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the environment state.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public CloudEnvironmentState State { get; set; }

        /// <summary>
        /// Gets or sets the continer image name.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "containerImage")]
        public string ContainerImage { get; set; }

        /// <summary>
        /// Gets or sets the environment seed info.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "seed")]
        public SeedInfo Seed { get; set; }

        /// <summary>
        /// Gets or sets the environment personalization info.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "personalization")]
        public PersonalizationInfo Personalization { get; set; }

        /// <summary>
        /// Gets or sets the environment connection info.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "connection")]
        public ConnectionInfo Connection { get; set; }

        /// <summary>
        /// Gets or sets the last active date and tiem.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "active")]
        public DateTime Active { get; set; }

        /// <summary>
        /// Gets or sets the fully-qualified Azure resource id of the Account object.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "accountId")]
        public string AccountId { get; set; }

        /// <summary>
        /// Gets or sets the environment sku name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "skuName")]
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the cloud environment Azure location.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the environment storage info.
        /// </summary>
        /// <remarks>
        /// Returned by back-end resource broker AllocateResult.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "storage")]
        public ResourceAllocation Storage { get; set; }

        /// <summary>
        /// Gets or sets the environment compute info.
        /// </summary>
        /// <remarks>
        /// Returned by back-end resource broker AllocateResult.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "compute")]
        public ResourceAllocation Compute { get; set; }
    }
}
