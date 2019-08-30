// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the system catalog.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="services"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        public static IServiceCollection AddStorageFileShareProvider(
            this IServiceCollection services,
            AppSettings appSettings)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(appSettings, nameof(appSettings));

            // Short circuit things if Resource Providers is being mocked
            if (appSettings.UseMocksForResourceProviders)
            {
                return services;
            }

            // Core services
            services.AddSingleton<IStorageProvider, StorageFileShareProvider>();
            services.AddSingleton<IStorageFileShareProviderHelper, StorageFileShareProviderHelper>();

            return services;
        }
    }
}
