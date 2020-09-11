// <copyright file="ServerHints.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts
{
    /// <summary>
    /// Forwarded server information. For example can be used to suggest the server is HTTPs
    /// </summary>
    public class ServerHints
    {
        /// <summary>
        /// Gets or sets the flag for Https servers.
        /// </summary>
        public bool UseHttps { get; set; }
    }
}
