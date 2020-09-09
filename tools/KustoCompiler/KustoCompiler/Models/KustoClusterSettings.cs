using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Models
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class KustoClusterSettings
    {
        public IList<string> AadScopes { get; set; }

        public string AzureClientId { get; set; }

        public string AzureAuthority { get; set; }
    }
}
