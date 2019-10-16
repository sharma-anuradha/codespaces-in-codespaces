// <copyright file="CollectedData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Base class representing data collected by monitors, job results etc.
    /// </summary>
    [DataContract]
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public abstract class CollectedData
    {
        /// <summary>
        /// Gets or sets the UTC timestamp at which the data is collected.
        /// </summary>
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or Sets the Name.
        /// </summary>
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public string Name { get; set; }
    }
}
