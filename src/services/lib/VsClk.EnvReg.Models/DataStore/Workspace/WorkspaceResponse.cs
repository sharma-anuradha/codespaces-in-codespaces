using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace VsClk.EnvReg.Models.DataStore.Workspace
{
    public class WorkspaceResponse : WorkspaceRequest
    {
        [JsonProperty(
            PropertyName = "id",
            Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty(
            PropertyName = "heartbeatInterval",
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TimeSpan HeartbeatInterval { get; set; }

        [JsonProperty(
            PropertyName = "sessionToken",
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SessionToken { get; set; }
    }
}
