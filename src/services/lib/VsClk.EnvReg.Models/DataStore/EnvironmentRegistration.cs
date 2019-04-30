using System;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore
{
    public class EnvironmentRegistration : TaggedEntity
    {
        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "friendlyName")]
        public string FriendlyName { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "created")]
        public DateTime Created { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "updated")]
        public DateTime Updated { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "containerImage")]
        public string ContainerImage { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "seed")]
        public SeedInfo Seed { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "connection")]
        public ConnectionInfo Connection { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "active")]
        public DateTime Active { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "platform")]
        public string Platform { get; set; }
    }

    public class SeedInfo
    {
        [JsonProperty(Required = Required.Default, PropertyName = "type")]
        public string SeedType { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "moniker")]
        public string SeedMoniker { get; set; }
    }

    public class ConnectionInfo
    {
        [JsonProperty(Required = Required.Default, PropertyName = "sessionId")]
        public string ConnectionSessionId { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "sessionPath")]
        public string ConnectionSessionPath { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "computeId")]
        public string ConnectionComputeId { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "computeTargetId")]
        public string ConnectionComputeTargetId { get; set; }
    }

    public enum StateInfo
    {
        Provisioning,
        Deleted,
        Available,
        Unavailable,
        Hibernating,
        WakingUp
    };

    public enum EnvType
    {
        cloudEnvironment,
        staticEnvironment
    };
}
