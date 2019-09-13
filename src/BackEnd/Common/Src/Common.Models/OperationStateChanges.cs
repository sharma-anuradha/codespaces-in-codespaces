// <copyright file="OperationStateChanges.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Operation State Changes for the resource.
    /// </summary>
    public class OperationStateChanges
    {
        /// <summary>
        /// Gets or sets the status that was updated.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState Status { get; set; }

        /// <summary>
        /// Gets or sets the time that the update occured.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the trigger of the status update.
        /// </summary>
        public string Trigger { get; set; }
    }
}
