// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        /// <param name="configureAzureSubscriptionCatalogOptions">The configuration callback for the azure sbuscription catalog settings.</param>
        /// <param name="configureSkuCatalogOptions">The configuration callback for the sku catalog settings.</param>
        /// <returns>The service collection instance.</returns>
        public static IServiceCollection AddSystemCatalog(
            this IServiceCollection serviceCollection,
            Action<AzureSubscriptionCatalogOptions> configureAzureSubscriptionCatalogOptions,
            Action<SkuCatalogOptions> configureSkuCatalogOptions)
        {
            Requires.NotNull(serviceCollection, nameof(serviceCollection));
            Requires.NotNull(configureAzureSubscriptionCatalogOptions, nameof(configureAzureSubscriptionCatalogOptions));
            Requires.NotNull(configureSkuCatalogOptions, nameof(configureSkuCatalogOptions));

            // The Azure Subscription Catalog
            serviceCollection.Configure(configureAzureSubscriptionCatalogOptions);
            serviceCollection.AddSingleton<IAzureSubscriptionCatalog, AzureSubscriptionCatalog>();

            // The SKU Catalog
            serviceCollection.Configure(configureSkuCatalogOptions);
            serviceCollection.AddSingleton<ISkuCatalog, SkuCatalog>();

            // The composite System Catlog
            serviceCollection.AddSingleton<ISystemCatalog, SystemCatalogProvider>();

            return serviceCollection;
        }
    }
}
