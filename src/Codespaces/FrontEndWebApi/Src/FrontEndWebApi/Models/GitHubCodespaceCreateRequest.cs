// <copyright file="GitHubCodespaceCreateRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
    }
}
