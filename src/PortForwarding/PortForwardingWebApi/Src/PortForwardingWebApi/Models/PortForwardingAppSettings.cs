// <copyright file="PortForwardingAppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
    }
}