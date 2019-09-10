﻿// <copyright file="ConnectionInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The environment connection info.
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// Gets or sets the connection session id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "sessionId")]
        public string ConnectionSessionId { get; set; }

        /// <summary>
        /// Gets or sets the connection session path.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "sessionPath")]
        public string ConnectionSessionPath { get; set; }

        /// <summary>
        /// Gets or sets the connection compute id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "computeId")]
        public string ConnectionComputeId { get; set; }

        /// <summary>
        /// Gets or sets the compute target id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "computeTargetId")]
        public string ConnectionComputeTargetId { get; set; }
    }
}
