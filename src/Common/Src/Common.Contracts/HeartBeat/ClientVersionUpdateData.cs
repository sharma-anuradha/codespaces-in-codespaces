// <copyright file="ClientVersionUpdateData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Stores data related to the status of VS Updates.
    /// </summary>
    [DataContract]
    public class ClientVersionUpdateData : CollectedData
    {
        /// <summary>
        /// Gets or sets the state of the update operation.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember]
        public JobState UpdateState { get; set; }

        /// <summary>
        /// Gets or sets the Visual Studio version number.
        /// </summary>
        [DataMember]
        public Version VsVersion { get; set; }
    }
}
