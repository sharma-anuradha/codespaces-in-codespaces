// <copyright file="ResourcePoolDefinitionRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource pool record.
    /// </summary>
    public class ResourcePoolDefinitionRecord
    {
        /// <summary>
        /// Gets or sets the definition code.
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the version definition code.
        /// </summary>
        [JsonProperty(PropertyName = "versionCode")]
        public string VersionCode { get; set; }

        /// <summary>
        /// Gets or sets the resource pool dimensions.
        /// </summary>
        [JsonProperty(PropertyName = "dimensions")]
        public IDictionary<string, string> Dimensions { get; set; }
    }
}
