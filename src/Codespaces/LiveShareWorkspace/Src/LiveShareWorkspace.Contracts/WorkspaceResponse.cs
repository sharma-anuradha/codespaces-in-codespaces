// <copyright file="WorkspaceResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable SA1600 // Elements should be documented

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace
{
    public class WorkspaceResponse : WorkspaceRequest
    {
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "heartbeatInterval", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TimeSpan HeartbeatInterval { get; set; }

        [JsonProperty(PropertyName = "sessionToken", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SessionToken { get; set; }
    }
}
