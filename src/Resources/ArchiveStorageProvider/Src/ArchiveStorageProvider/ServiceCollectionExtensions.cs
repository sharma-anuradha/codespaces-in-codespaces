// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Mocks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the archive storage provider.
    /// </summary>
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the capacity manager to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="mocksSettings">The mocks settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddArchiveStorageProvider(
            this IServiceCollection services,
            MocksSettings mocksSettings = null)
        {
            if (mocksSettings?.UseMocksForResourceProviders == true)
            {
                services.AddSingleton<IArchiveStorageProvider, MockArchiveStorageProvider>();
            }
            else
            {
                services.AddSingleton<IArchiveStorageProvider, ArchiveStorageProvider>();
                services.AddSingleton<IAsyncWarmup, ArchiveStorageWarmup>();
            }

            return services;
        }

        /// <summary>
        /// Adds the export capacity manager to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="mocksSettings">The mocks settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddExportStorageProvider(
            this IServiceCollection services,
            MocksSettings mocksSettings = null)
        {
            if (mocksSettings?.UseMocksForResourceProviders == true)
            {
                services.AddSingleton<IExportStorageProvider, MockExportStorageProvider>();
            }
            else
            {
                services.AddSingleton<IExportStorageProvider, ExportStorageProvider>();
                services.AddSingleton<IAsyncWarmup, ExportStorageWarmup>();
            }

            return services;
        }
    }
}
