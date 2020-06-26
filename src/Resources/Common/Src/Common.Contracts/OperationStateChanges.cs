// <copyright file="OperationStateChanges.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Operation State Changes for the resource.
    /// </summary>
    public class OperationStateChanges
    {
        /// <summary>
        /// Gets or sets the status that was updated.
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState Status { get; set; }

        /// <summary>
        /// Gets or sets the time that the update occured.
        /// </summary>
        [JsonProperty(PropertyName = "time")]
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the trigger of the status update.
        /// </summary>
        [JsonProperty(PropertyName = "trigger")]
        public string Trigger { get; set; }
    }
}
