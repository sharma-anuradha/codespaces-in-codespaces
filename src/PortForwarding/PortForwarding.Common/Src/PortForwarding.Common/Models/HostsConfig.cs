// <copyright file="HostsConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models
{
    /// <summary>
    /// List of hosts with associated ssl certificate secret name.
    /// </summary>
    public class HostsConfig
    {
        /// <summary>
        /// Gets or sets ssl certificate secret name.
        /// </summary>
        public string CertificateSecretName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether environment id based hosts are allowed.
        /// </summary>
        public bool AllowEnvironmentIdBasedHosts { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of port forwarding hosts.
        /// </summary>
        public IEnumerable<string> Hosts { get; set; }
    }
}