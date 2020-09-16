// <copyright file="SystemConfigurationRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models
{
    /// <summary>
    /// Configuration Record.
    /// </summary>
    public class SystemConfigurationRecord : TaggedEntity
    {
        /// <summary>
        /// Gets or sets the value of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the configuration comment.
        /// </summary>
        [JsonProperty(PropertyName = "comment")]
        public string Comment { get; set; }
    }
}
