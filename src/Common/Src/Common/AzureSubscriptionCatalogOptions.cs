// <copyright file="AzureSubscriptionCatalogOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// An options object the Azure subscription catalog.
    /// </summary>
    public class AzureSubscriptionCatalogOptions
    {
        /// <summary>
        /// Gets or sets the default application service principal.
        /// </summary>
        public ServicePrincipalSettings ApplicationServicePrincipal { get; set; }

        /// <summary>
        /// Gets or sets the data plane settings for the subscription catalog.
        /// </summary>
        public DataPlaneSettings DataPlaneSettings { get; set; }
    }
}
