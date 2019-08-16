using Microsoft.Azure.Management.ContainerRegistry.Fluent.Models;
using Microsoft.Azure.Management.Sql.Fluent;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models
{
    /// <summary>
    /// JSON body properties from RPSaaS
    /// </summary>
    public class BillingAccountInput
    {
        [JsonProperty(Required = Required.Default, PropertyName = "properties")]
        public BillingAccountInputProperties Properties { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "provisioningState")]
        public string ProvisioningState { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "tags")]
        public IDictionary<string, string> Tags { get; set; }
    }

    public class BillingAccountInputProperties
    {
        [JsonProperty(Required = Required.Default, PropertyName = "sku")]
        public Sku Plan { get; set; }
    }

    /// <summary>
    /// Azure standard Sku model
    /// </summary>
    public class Sku
    {
        [JsonProperty(Required = Required.Default, PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "tier")]
        public string Tier { get; set; }
    }
}
