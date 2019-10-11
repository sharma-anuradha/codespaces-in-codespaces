// <copyright file="JobResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// Stores result for job.
    /// </summary>
    public class JobResult : CollectedData
    {
        /// <summary>
        /// Gets or sets the environment id.
        /// </summary>
        [JsonProperty("environmentId")]
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets the job state.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("jobState")]
        public JobState JobState { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        [JsonProperty("endTime")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the errors.
        /// </summary>
        [JsonProperty("Errors")]
        public string[] Errors { get; set; }

        /// <summary>
        /// Gets or sets the task results.
        /// </summary>
        [JsonProperty("operationResults")]
        public JobOperationResult[] OperationResults { get; set; }
    }
}
