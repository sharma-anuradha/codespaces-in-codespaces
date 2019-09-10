﻿// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
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
        /// <see cref="IServiceCollection"/> extensions for the storage provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="mocksSettings">The mocks settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddStorageFileShareProvider(
            this IServiceCollection services,
            MocksSettings mocksSettings = null)
        {
            Requires.NotNull(services, nameof(services));

            // Short circuit things if Resource Providers is being mocked
            if (mocksSettings?.UseMocksForResourceProviders == true)
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
