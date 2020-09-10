// <copyright file="PortForwardingHeaders.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common
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
        /// Gets header name for environment id.
        /// </summary>
        public static string EnvironmentId { get => "X-VSOnline-Forwarding-EnvironmentId"; }

        /// <summary>
        /// Gets header name for port.
        /// </summary>
        public static string Port { get => "X-VSOnline-Forwarding-Port"; }

        /// <summary>
        /// Gets header name for token.
        /// </summary>
        public static string Token { get => "X-VSOnline-Forwarding-Token"; }

        /// <summary>
        /// Gets header name for authentication.
        /// </summary>
        public static string Authentication { get => "X-VSOnline-Authentication"; }

        /// <summary>
        /// Gets header name for original url header.
        /// </summary>
        public static string OriginalUrl { get => "X-Original-URL"; }

        /// <summary>
        /// Gets header name for codespace state.
        /// </summary>
        public static string CodespaceState { get => "X-Codespaces-State"; }
    }
}
