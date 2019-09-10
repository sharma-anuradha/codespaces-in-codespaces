// <copyright file="SkuCatalogOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// An options object the Azure subscription catalog.
    /// </summary>
    public class SkuCatalogOptions
    {
        /// <summary>
        /// Gets or sets the azure subscription catalog settings.
        /// </summary>
        public SkuCatalogSettings Settings { get; set; } = new SkuCatalogSettings();
    }
}
