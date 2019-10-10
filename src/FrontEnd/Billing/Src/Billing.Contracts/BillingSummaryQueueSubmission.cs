// <copyright file="BillingSummaryQueueSubmission.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Contract for submission 
    /// </summary>
    public class BillingSummaryQueueSubmission
    {
        [JsonProperty(Required = Required.Always, PropertyName = "partitionId")]
        public string PartitionKey { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "batchId")]
        public string BatchId { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(
                this, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
        }
    }
}
