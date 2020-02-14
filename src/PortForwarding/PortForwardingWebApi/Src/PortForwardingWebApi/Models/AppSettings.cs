// <copyright file="AppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class AppSettings : AppSettingsBase
    {
        /// <summary>
        /// Gets or sets the port forwarding service configuration.
        /// </summary>
        public PortForwardingAppSettings PortForwarding { get; set; } = default!;
    }
}
