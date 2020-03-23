// <copyright file="PortForwardingHeaders.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi
{
    /// <summary>
    /// Constants for port forwarding header names.
    /// </summary>
    public static class PortForwardingHeaders
    {
        /// <summary>
        /// Gets header name for workspace id.
        /// </summary>
        public static string WorkspaceId { get => "X-VSOnline-Forwarding-WorkspaceId"; }

        /// <summary>
        /// Gets header name for port.
        /// </summary>
        public static string Port { get => "X-VSOnline-Forwarding-Port"; }

        /// <summary>
        /// Gets header name for port.
        /// </summary>
        public static string Token { get => "X-VSOnline-Forwarding-Token"; }
    }
}
