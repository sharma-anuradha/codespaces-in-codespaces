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
        /// Gets or sets the name of the blob container that the Resource Broker can use for distributed leases.
        /// </summary>
        public string LeaseContainerName { get; set; }

        /// <summary>
        /// Gets or sets the name of the blob container that the Resource Broker can use for file share template blobs.
        /// </summary>
        public string FileShareTemplateContainerName { get; set; }

        /// <summary>
        /// Gets or sets the blob name of the template blob used for populating initial file shares.
        /// </summary>
        public string FileShareTemplateBlobName { get; set; }
    }
}
