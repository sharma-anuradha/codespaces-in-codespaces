// <copyright file="JobResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Stores result for job.
    /// </summary>
    [DataContract]
    public class JobResult : CollectedData
    {
        /// <summary>
        /// Gets or sets a value indicating the job id.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the job state.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember(IsRequired = true, EmitDefaultValue = true)]
        public JobState JobState { get; set; }

        /// <summary>
        /// Gets or sets the job end time.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the timeout for the currently running job step.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime? Timeout { get; set; }

        /// <summary>
        /// Gets or sets the job errors.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<string> Errors { get; set; }

        /// <summary>
        /// Gets or sets the job operation results.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<JobOperationResult> OperationResults { get; set; }
    }
}
