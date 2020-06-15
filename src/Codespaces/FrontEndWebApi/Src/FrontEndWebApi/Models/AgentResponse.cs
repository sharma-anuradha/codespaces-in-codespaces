// <copyright file="AgentResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// The response object to describe downloadable agent binaries.
    /// </summary>
    public class AgentResponse
    {
        /// <summary>
        /// Gets or sets the Name of the agent.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the location for downloading the agent.
        /// </summary>
        public string AssetUri { get; set; }

        /// <summary>
        /// Gets or sets the runtime platform for which the agent is compatible.
        /// </summary>
        public string Family { get; set; }
    }
}
