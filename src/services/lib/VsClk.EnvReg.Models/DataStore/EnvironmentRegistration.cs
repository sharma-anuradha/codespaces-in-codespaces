using System;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore
{
    public class EnvironmentRegistration : TaggedEntity
    {
        [JsonProperty(Required = Required.Always, PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "friendlyName")]
        public string FriendlyName { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "created")]
        public DateTime Created { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "updated")]
        public DateTime Updated { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "active")]
        public DateTime Active { get; set; }
    }
}
