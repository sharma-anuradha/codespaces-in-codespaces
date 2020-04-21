// <copyright file="PartialEnvironmentSessionDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models
{
    /// <summary>
    /// Port forwarding session parameters.
    /// </summary>
    public class PartialEnvironmentSessionDetails : PortForwardingSessionDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartialEnvironmentSessionDetails"/> class.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="port">The shared port.</param>
        public PartialEnvironmentSessionDetails(string environmentId, int port)
            : base(port)
        {
            EnvironmentId = !string.IsNullOrWhiteSpace(environmentId) ? environmentId : throw new ArgumentNullException(nameof(environmentId));
        }

        /// <summary>
        /// Gets the environment id.
        /// </summary>
        public string EnvironmentId { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is PartialEnvironmentSessionDetails parameters &&
                   base.Equals(obj) &&
                   EnvironmentId == parameters.EnvironmentId;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Port, EnvironmentId);
        }

        /// <summary>
        /// Deconstructs current instance for easier use in pattern matching.
        /// </summary>
        /// <param name="environmentId">The current environment id.</param>
        /// <param name="port">The port.</param>
        public void Deconstruct(out string environmentId, out int port)
        {
            environmentId = EnvironmentId;
            port = Port;
        }
    }
}