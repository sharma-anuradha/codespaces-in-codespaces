// <copyright file="ConnectionInfoBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// Represents a Live Share connection.
    /// </summary>
    public class ConnectionInfoBody
    {
        /// <summary>
        /// Gets or sets the Live Share session id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "sessionId")]
        public string ConnectionSessionId { get; set; }

        /// <summary>
        /// Gets or sets the Live Share session path.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "sessionPath")]
        public string ConnectionSessionPath { get; set; }

        /// <summary>
        /// Gets or sets the Live Share service URI that should be used
        /// to connect to this environment.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "serviceUri")]
        public string ConnectionServiceUri { get; set; }
    }
}