// <copyright file="SystemCatalogProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog
{
    /// <inheritdoc/>
    public class SystemCatalogProvider : ISystemCatalog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCatalogProvider"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        /// <param name="skuCatalog">The sku catalog.</param>
        public SystemCatalogProvider(
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            ISkuCatalog skuCatalog)
        {
            AzureSubscriptionCatalog = Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
        }

        /// <inheritdoc/>
        public IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        /// <inheritdoc/>
        public ISkuCatalog SkuCatalog { get; }
    }
}
