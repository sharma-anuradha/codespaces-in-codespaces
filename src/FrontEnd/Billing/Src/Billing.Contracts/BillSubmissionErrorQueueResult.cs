// <copyright file="BillSubmissionErrorQueueResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class BillSubmissionErrorQueueResult
    {
        /// <summary>
        /// This maps to which partition key has the error.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "partitionId")]
        public string PartitionId { get; set; }

    }
}
