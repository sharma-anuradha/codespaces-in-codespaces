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

        [JsonProperty(Required = Required.Default, PropertyName = "personalization")]
        public PersonalizationInfo Personalization { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "connection")]
        public ConnectionInfo Connection { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "storage")]
        public StorageInfo Storage { get; set; }

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

        [JsonProperty(Required = Required.Default, PropertyName = "gitConfig")]
        public GitConfig GitConfig { get; set; }
    }

    public class PersonalizationInfo
    {
        [JsonProperty(Required = Required.Default, PropertyName = "dotfilesRepository")]
        public string DotfilesRepository { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "dotfilesTargetPath")]
        public string DotfilesTargetPath { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "dotfilesInstallCommand")]
        public string DotfilesInstallCommand { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "preferredShells")]
        public string[] PreferredShells { get; set; }
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

    public class StorageInfo
    {
        [JsonProperty(Required = Required.Default, PropertyName = "storageId")]
        public string StorageId { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "storageKind")]
        public string StorageKind { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "fileShareName")]
        public string FileShareId { get; set; }
    }

    public class GitConfig
    {
        [JsonProperty(Required = Required.Default, PropertyName = "userName")]
        public string UserName { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "userEmail")]
        public string UserEmail { get; set; }
    }

    public enum StateInfo
    {
        /// <summary>
        /// Readying the environment.
        /// </summary>
        Provisioning,

        /// <summary>
        /// Environment is ready and available to connect.
        /// </summary>
        Available,

        /// <summary>
        /// Environment is ready but waiting for the host to connect.
        /// </summary>
        Awaiting,

        /// <summary>
        /// Environment is unavailable connect. There is no recovery path.
        /// </summary>
        Unavailable,
    };

    public enum EnvType
    {
        CloudEnvironment,
        StaticEnvironment
    };
}
