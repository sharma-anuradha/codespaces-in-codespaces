using System;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Service document model
    /// </summary>
    public class ServiceDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("serviceInfo")]
        public object ServiceInfo { get; set; }

        [JsonProperty("metrics")]
        public PresenceServiceMetrics Metrics { get; set; }

        [JsonProperty("lastUpdate")]
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Contact document model
    /// </summary>
    public class ContactDataDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("changeId")]
        public string ChangeId { get; set; }

        [JsonProperty("serviceId")]
        public string ServiceId { get; set; }

        [JsonProperty("connectionId")]
        public string ConnectionId { get; set; }

        [JsonProperty("properties")]
        public string[] Properties { get; set; }

        [JsonProperty("updateType")]
        public ContactUpdateType UpdateType { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("serviceConnections")]
        public object ServiceConnections { get; set; }

        [JsonProperty("lastUpdate")]
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Message docuemnt model
    /// </summary>
    public class MessageDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("contactId")]
        public string ContactId { get; set; }

        [JsonProperty("targetContactId")]
        public string TargetContactId { get; set; }

        [JsonProperty("targetConnectionId")]
        public string TargetConnectionId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("body")]
        public object Body { get; set; }

        [JsonProperty("sourceId")]
        public string SourceId { get; set; }

        [JsonProperty("lastUpdate")]
        public DateTime LastUpdate { get; set; }
    }
}
