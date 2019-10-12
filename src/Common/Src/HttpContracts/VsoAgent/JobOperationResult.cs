// <copyright file="JobOperationResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// Store result for tasks in job.
    /// </summary>
    [DataContract]
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class JobOperationResult
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether task was successfull.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool Succeeded { get; set; }
    }
}
