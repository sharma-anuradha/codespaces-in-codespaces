// <copyright file="ISkuCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions
{
    /// <summary>
    /// The Cloud Environment SKU catalog.
    /// </summary>
    public interface ISkuCatalog
    {
        /// <summary>
        /// Gets the list of Cloud Environment SKUs.
        /// </summary>
        IEnumerable<ICloudEnvironmentSku> CloudEnvironmentSkus { get; }

        /// <summary>
        /// Lookup a cloud environment SKU by name.
        /// </summary>
        /// <param name="skuName">The SKU name.</param>
        /// <param name="cloudEnvironmentSku">The output cloud environment SKU.</param>
        /// <returns>True if the SKU exists, otherwise false.</returns>
        bool TryGetCloudEnvironmentSku(string skuName, out ICloudEnvironmentSku cloudEnvironmentSku);
    }
}
