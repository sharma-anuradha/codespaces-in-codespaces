using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace VsClk.EnvReg.Models.DataStore.Compute
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ComputeTargetResponse
    {
        [JsonProperty(Required = Required.Always, PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "providerKind")]
        public string ProviderKind { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "created")]
        public string Created { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "updated")]
        public string Updated { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "properties")]
        public Dictionary<string, dynamic> Properties { get; set; }
    }
}
