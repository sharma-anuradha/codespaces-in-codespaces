// <copyright file="SystemCatalogProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    public class SystemCatalogProvider : ISystemCatalog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCatalogProvider"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        /// <param name="skuCatalog">The sku catalog.</param>
        /// <param name="planSkuCatalog">The plan sku catalog.</param>
        public SystemCatalogProvider(
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            ISkuCatalog skuCatalog,
            IPlanSkuCatalog planSkuCatalog)
        {
            AzureSubscriptionCatalog = Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            PlanSkuCatalog = Requires.NotNull(planSkuCatalog, nameof(planSkuCatalog));
        }

        /// <inheritdoc/>
        public IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        /// <inheritdoc/>
        public ISkuCatalog SkuCatalog { get; }

        /// <inheritdoc/>
        public IPlanSkuCatalog PlanSkuCatalog { get; }
    }
}
