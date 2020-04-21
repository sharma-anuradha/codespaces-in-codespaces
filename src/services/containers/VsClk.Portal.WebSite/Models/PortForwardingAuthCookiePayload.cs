using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Models
{
    public class PortForwardingAuthCookiePayload
    {
        [JsonProperty("timeStamp")]
        public string TimeStamp { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("environmentId")]
        public string EnvironmentId { get; set; }

        [JsonProperty("connectionSessionId")]
        public string ConnectionSessionId { get; set; }

        /*Properties will be decrypted in order, better to have token as the last one for security purpose.
        **by default "order = -1" for JSON properties, wee need to make it "1" to be the last one.
        */
        [JsonProperty(PropertyName = "cascadeToken", Order = 1)]
        public string CascadeToken { get; set; }
    }
}
