using System.Text.RegularExpressions;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models
{
    public class EnvironmentRegistrationInput
    {
        public string Type { get; set; }

        public string FriendlyName { get; set; }

        public bool CreateFileShare { get; set; }

        public SeedInfoInput Seed { get; set; }
        public string ContainerImage { get; set; }
        public ConnectionInfoInput Connection { get; set; }
        public string Platform { get; set; }
    }
    public class SeedInfoInput
    {
        [JsonProperty(Required = Required.Default, PropertyName = "type")]
        public string SeedType { get; set; }
        [JsonProperty(Required = Required.Default, PropertyName = "moniker")]
        public string SeedMoniker { get; set; }
    }
    public class ConnectionInfoInput
    {
        [JsonProperty(Required = Required.Default, PropertyName = "sessionId")]
        public string ConnectionSessionId { get; set; }
        [JsonProperty(Required = Required.Default, PropertyName = "sessionPath")]
        public string ConnectionSessionPath { get; set; }
    }
}