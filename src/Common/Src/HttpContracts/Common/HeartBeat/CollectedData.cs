// <copyright file="CollectedData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// Base class representing data collected by monitors, job results etc.
    /// </summary>
    public abstract class CollectedData
    {
        /// <summary>
        /// Gets or sets the UTC timestamp at which the data is collected.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or Sets the Name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
