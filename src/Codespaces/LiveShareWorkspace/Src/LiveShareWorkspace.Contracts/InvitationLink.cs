// <copyright file="InvitationLink.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace.Contracts
{
    /// <summary>
    /// Invitation link object received from the service.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class InvitationLink
    {
        /// <summary>
        /// Gets or sets unique id for the invitation link.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets link used to join the session.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string JoinLink { get; set; }
    }
}
