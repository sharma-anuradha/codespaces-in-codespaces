// <copyright file="PortForwardingSessionDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models
{
    /// <summary>
    /// Port forwarding session details.
    /// </summary>
    public class PortForwardingSessionDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PortForwardingSessionDetails"/> class.
        /// </summary>
        /// <param name="port">The shared port.</param>
        public PortForwardingSessionDetails(int port)
        {
            Port = port;
        }

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is PortForwardingSessionDetails details &&
                   Port == details.Port;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Port);
        }

        /// <summary>
        /// Deconstructs current instance for easier use in pattern matching.
        /// </summary>
        /// <param name="port">The port.</param>
        public void Deconstruct(out int port)
        {
            port = Port;
        }
    }
}