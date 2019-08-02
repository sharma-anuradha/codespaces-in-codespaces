// <copyright file="AzureSubscriptionCatalogOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings
{
    /// <summary>
    /// An options object the Azure subscription catalog.
    /// </summary>
    public class AzureSubscriptionCatalogOptions
    {
        /// <summary>
        /// Gets or sets the azure subscription catalog settings.
        /// </summary>
        public AzureSubscriptionCatalogSettings Settings { get; set; } = new AzureSubscriptionCatalogSettings();
    }
}
