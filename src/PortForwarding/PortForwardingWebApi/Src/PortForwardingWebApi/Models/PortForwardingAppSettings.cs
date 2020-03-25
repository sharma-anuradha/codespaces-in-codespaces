// <copyright file="PortForwardingAppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class PortForwardingAppSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether non-critical background tasks are
        /// disabled for local development.
        /// </summary>
        public bool DisableBackgroundTasksForLocalDevelopment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to mock creation of kubernetes resources during development.
        /// </summary>
        public bool UseMockKubernetesMappingClientInDevelopment { get; set; }

        /// <summary>
        /// Gets or sets the list of port forwarding hosts.
        /// </summary>
        public IEnumerable<HostsConfig> HostsConfigs { get; set; } = default!;

        /// <summary>
        /// Gets or sets the LiveShare API endpoint to be used.
        /// </summary>
        public string VSLiveShareApiEndpoint { get; set; } = default!;
    }
}