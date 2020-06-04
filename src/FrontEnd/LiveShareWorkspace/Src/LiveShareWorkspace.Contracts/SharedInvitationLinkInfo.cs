// <copyright file="SharedInvitationLinkInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace.Contracts
{
    /// <summary>
    /// Invitation link info provided to the agent to create an access control link.
    /// </summary>
    public class SharedInvitationLinkInfo
    {
        /// <summary>
        /// Gets or sets the parent workspace id.
        /// </summary>
        [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public string WorkspaceId { get; set; }

        /// <summary>
        /// Gets or sets the list of guest users allowed to connect using this link.
        /// </summary>
        [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public string[] GuestUsers { get; set; }
    }
}
