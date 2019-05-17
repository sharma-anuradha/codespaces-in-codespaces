using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore
{
    public class FileShare : TaggedEntity
    {
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        public FileShareEnvironmentInfo EnvironmentInfo { get; set; }
    }

    public class FileShareEnvironmentInfo
    {
        [JsonProperty(Required = Required.Always, PropertyName = "friendlyName")]
        public string FriendlyName { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "ownerId")]
        public string OwnerId { get; set; }
    }
}
