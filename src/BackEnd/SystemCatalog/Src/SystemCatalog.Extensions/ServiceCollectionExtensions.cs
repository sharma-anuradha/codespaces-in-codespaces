// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the system catalog.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the system catalog to the service collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="azureSubscriptionCatalogSettings">The azure sbuscription catalog settings.</param>
        /// <param name="skuCatalogSettings">The sku catalog settings.</param>
        /// <returns>The service collection instance.</returns>
        public static IServiceCollection AddSystemCatalog(
            this IServiceCollection serviceCollection,
            AzureSubscriptionCatalogSettings azureSubscriptionCatalogSettings,
            SkuCatalogSettings skuCatalogSettings)
        {
            Requires.NotNull(serviceCollection, nameof(serviceCollection));
            Requires.NotNull(azureSubscriptionCatalogSettings, nameof(azureSubscriptionCatalogSettings));
            Requires.NotNull(skuCatalogSettings, nameof(skuCatalogSettings));

            // The Azure Subscription Catalog
            serviceCollection.Configure<AzureSubscriptionCatalogOptions>(x => x.Settings = azureSubscriptionCatalogSettings);
            serviceCollection.AddSingleton<IAzureSubscriptionCatalog, AzureSubscriptionCatalog>();

            // The SKU Catalog
            serviceCollection.Configure<SkuCatalogOptions>(x => x.Settings = skuCatalogSettings);
            serviceCollection.AddSingleton<ISkuCatalog, SkuCatalog>();

            // The composite System Catlog
            serviceCollection.AddSingleton<ISystemCatalog, SystemCatalogProvider>();

            return serviceCollection;
        }
    }
}
