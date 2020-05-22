// <copyright file="PlanSkuCatalogOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// An options object the Azure subscription catalog.
    /// </summary>
    public class PlanSkuCatalogOptions
    {
        /// <summary>
        /// Gets or sets the catalog settings.
        /// </summary>
        public PlanSkuCatalogSettings Settings { get; set; } = new PlanSkuCatalogSettings();
    }
}
