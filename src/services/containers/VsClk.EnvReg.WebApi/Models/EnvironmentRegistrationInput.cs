using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models
{
    public class EnvironmentRegistrationInput
    {
        [Required]
        public string Type { get; set; }

        [Required]
        public string FriendlyName { get; set; }

        public bool CreateFileShare { get; set; }

        public SeedInfoInput Seed { get; set; }

        public PersonalizationInfo Personalization { get; set; }

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

        [JsonProperty(Required = Required.Default, PropertyName = "gitConfig")]
        public GitConfigInput GitConfig { get; set; }
    }

    public class PersonalizationInfo
    {
        [JsonProperty(Required = Required.Default, PropertyName = "dotfilesRepository")]
        public string DotfilesRepository { get; set; }
    }

    public class ConnectionInfoInput
    {
        [JsonProperty(Required = Required.Default, PropertyName = "sessionId")]
        public string ConnectionSessionId { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "sessionPath")]
        public string ConnectionSessionPath { get; set; }
    }

    public class GitConfigInput
    {
        [JsonProperty(Required = Required.Default, PropertyName = "userName")]
        public string UserName { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "userEmail")]
        public string UserEmail { get; set; }
    }
}