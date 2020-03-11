// <copyright file="Info.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Controllers
{
    /// <summary>
    /// Connection details passed to port forwarding agent - used to establish LS connection.
    /// </summary>
    public class Info
    {
        /// <summary>
        /// Gets or sets LS Workspace ID.
        /// </summary>
        public string? Message { get; set; }
    }
}
