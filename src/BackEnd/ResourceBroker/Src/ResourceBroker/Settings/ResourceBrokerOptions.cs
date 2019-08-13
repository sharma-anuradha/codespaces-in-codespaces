// <copyright file="ResourceBrokerOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings
{
    /// <summary>
    /// An options object for the Resource Broker settings.
    /// </summary>
    public class ResourceBrokerOptions
    {
        /// <summary>
        /// Gets or sets the Resource Broker settings.
        /// </summary>
        public ResourceBrokerSettings Settings { get; set; } = new ResourceBrokerSettings();
    }
}
