// <copyright file="WorkspaceRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

#pragma warning disable SA1600 // Elements should be documented

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace
{
    /// <summary>
    /// Defines the ways that an RPC caller can connect to a workspace when joining.
    /// </summary>
    /// <remarks>
    /// Changes to this enum may require corresponding updates to the available values
    /// offered for users to select in client settings.
    /// </remarks>
    public enum ConnectionMode
    {
        /// <summary>
        /// Try connecting directly, if that fails then try connecting via a relay.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Direct (peer-to-peer) TCP connection; may be on the same local network,
        /// through VPN, or through NAT traversal.
        /// </summary>
        Direct,

        /// <summary>
        /// Connection through a cloud relay server.
        /// </summary>
        Relay,

        /// <summary>
        /// Indicates the workspace to be joined is one that is hosted locally by the
        /// agent being called; the callee must not forward the join request to another
        /// remote agent.
        /// </summary>
        /// <remarks>
        /// This mode is not to be specified by a user, therefore it should be hidden
        /// from user settings. It is used internally based on application or agent logic
        /// when the target workspace is known to be hosted by the agent being called.
        /// </remarks>
        Local,
    }

    public class WorkspaceRequest
    {
        [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public string Name { get; set; }

        [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public string[] ConnectLinks { get; set; }

        [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public string[] HostPublicKeys { get; set; }

        [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        [JsonConverter(typeof(StringEnumConverter))]
        public ConnectionMode ConnectionMode { get; set; }

        [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public bool AreAnonymousGuestsAllowed { get; set; }

        [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public bool? IsHostConnected { get; set; }

        [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public DateTime ExpiresAt { get; set; }
    }
}
