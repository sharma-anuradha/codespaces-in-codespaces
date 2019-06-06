using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace VsClk.EnvReg.Models.DataStore.Compute
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ComputeServiceRequest
    {
        public IList<EnvironmentVariable> EnvironmentVariables { get; set; }

        public StorageSpecification Storage { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class EnvironmentVariable
    {
        public EnvironmentVariable(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public string Value { get; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class StorageSpecification
    {
        public string FileShareName { get; set; }
    }

}
