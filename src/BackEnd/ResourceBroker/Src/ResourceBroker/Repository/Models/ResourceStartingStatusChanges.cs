using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceStartingStatusChanges
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Time { get; set; }
    }
}
