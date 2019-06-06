using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace VsClk.EnvReg.Models.DataStore.Compute
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ComputeResourceResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "providerKind")]
        public string ProviderKind { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "updated")]
        public string Updated { get; set; }

        [JsonProperty(PropertyName = "created")]
        public string Created { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public Dictionary<string, dynamic> Properties { get; set; }
    }
}
