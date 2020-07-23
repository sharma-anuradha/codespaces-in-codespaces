// <copyright file="AgentRegistration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts
{
    /// <summary>
    /// Port forwarding agent mapping registration sent by the PFA during startup.
    /// </summary>
    public class AgentRegistration
    {
        /// <summary>
        /// Gets or sets PFA Agent Kubernetes pod name.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets PFA Agent Kubernetes pod Uid.
        /// </summary>
        public string Uid { get; set; } = default!;
    }
}