// <copyright file="SystemCatalogServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the system catalog.
    /// </summary>
    public static class SystemCatalogServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the system catalog to the service collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="dataPlaneSettings">The data plane settings.</param>
        /// <param name="skuCatalogSettings">The sku catalog settings.</param>
        /// <param name="planSkuCatalogSettings">The plan sku catalog settings.</param>
        /// <param name="quotaFamilySettings"> The quota family settings.</param>
        /// <param name="applicationServicePrincipalSettings">The default application service principal. Can be null.</param>
        /// <returns>The service collection instance.</returns>
        public static IServiceCollection AddSystemCatalog(
            this IServiceCollection serviceCollection,
            DataPlaneSettings dataPlaneSettings,
            SkuCatalogSettings skuCatalogSettings,
            PlanSkuCatalogSettings planSkuCatalogSettings,
            IDictionary<string, IDictionary<string, int>> quotaFamilySettings,
            ServicePrincipalSettings applicationServicePrincipalSettings)
        {
            Requires.NotNull(serviceCollection, nameof(serviceCollection));
            Requires.NotNull(dataPlaneSettings, nameof(dataPlaneSettings));
            Requires.NotNull(skuCatalogSettings, nameof(skuCatalogSettings));

            // The Azure Subscription Catalog
            serviceCollection.Configure<AzureSubscriptionCatalogOptions>(
                options =>
                {
                    options.ApplicationServicePrincipal = applicationServicePrincipalSettings;
                    options.DataPlaneSettings = dataPlaneSettings;
                });
            serviceCollection.AddSingleton<IAzureSubscriptionCatalog, AzureSubscriptionCatalog>();

            // The SKU Catalog
            serviceCollection.Configure<SkuCatalogOptions>(x => x.Settings = skuCatalogSettings);
            serviceCollection.AddSingleton<ISkuCatalog, SkuCatalog>();

            // The Plan SKU Catalog
            serviceCollection.Configure<PlanSkuCatalogOptions>(x => x.Settings = planSkuCatalogSettings);
            serviceCollection.AddSingleton<IPlanSkuCatalog, PlanSkuCatalog>();

            // Add the Quota Family catalog
            serviceCollection.Configure<QuotaFamilySettingsOptions>(x => x.Settings = quotaFamilySettings);
            serviceCollection.AddSingleton<IQuotaFamilyCatalog, QuotaFamilyCatalog>();

            // The composite System Catlog
            serviceCollection.AddSingleton<ISystemCatalog, SystemCatalogProvider>();

            return serviceCollection;
        }
    }
}
