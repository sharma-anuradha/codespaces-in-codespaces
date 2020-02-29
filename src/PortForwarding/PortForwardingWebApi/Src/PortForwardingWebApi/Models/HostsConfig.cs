﻿// <copyright file="HostsConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models
{
    /// <summary>
    /// List of hosts with associated ssl certificate secret name.
    /// </summary>
    public class HostsConfig
    {
        /// <summary>
        /// Gets or sets ssl certificate secret name.
        /// </summary>
        public string CertificateSecretName { get; set; } = default!;

        /// <summary>
        /// Gets or sets the list of port forwarding hosts.
        /// </summary>
        public IEnumerable<string> Hosts { get; set; } = default!;
    }
}