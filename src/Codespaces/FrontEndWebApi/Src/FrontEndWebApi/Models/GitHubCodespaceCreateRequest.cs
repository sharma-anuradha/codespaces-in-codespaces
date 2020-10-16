// <copyright file="GitHubCodespaceCreateRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Represents the request to create a codespace, as required by GitHub API.
    /// </summary>
    public class GitHubCodespaceCreateRequest
    {
        [JsonProperty(Required = Required.Always, PropertyName = "repository_id")]
        public int RepositoryId { get; set; }

        [JsonProperty(Required = Required.AllowNull, PropertyName = "ref", NullValueHandling = NullValueHandling.Ignore)]
        public string Reference { get; set; }

        [JsonProperty(Required = Required.AllowNull, PropertyName = "location", NullValueHandling = NullValueHandling.Ignore)]
        public string Location { get; set; }

        [JsonProperty(Required = Required.AllowNull, PropertyName = "sku", NullValueHandling = NullValueHandling.Ignore)]
        public string Sku { get; set; }

        [JsonProperty(Required = Required.AllowNull, PropertyName = "vscs_target", NullValueHandling = NullValueHandling.Ignore)]
        public Targets? Target { get; set; } = null;

        [JsonProperty(Required = Required.Always, PropertyName = "fork_if_needed")]
        public bool ForkIfNeeded { get; set; } = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public enum Targets
        {
            [EnumMember(Value = "development")]
            Development,
            [EnumMember(Value = "ppe")]
            Staging,
            [EnumMember(Value = "production")]
            Production,
        }
    }
}
