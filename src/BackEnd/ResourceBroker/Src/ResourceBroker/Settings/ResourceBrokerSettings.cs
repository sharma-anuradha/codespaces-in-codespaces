// <copyright file="ResourceBrokerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings
{
    /// <summary>
    /// Settings for the resource broker to use at runtime.
    /// </summary>
    public class ResourceBrokerSettings
    {
        /// <summary>
        /// Gets or sets the name of the blob container that the Resource Broker can use.
        /// </summary>
        public string BlobContainerName { get; set; }
    }
}
