// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
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
        /// <param name="storageProviderSettings">The storage provider settings.</param>
        /// <param name="mocksSettings">The mocks settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddStorageFileShareProvider(
            this IServiceCollection services,
            StorageProviderSettings storageProviderSettings,
            MocksSettings mocksSettings = null)
        {
            Requires.NotNull(services, nameof(services));

            // Short circuit things if Resource Providers is being mocked
            if (mocksSettings?.UseMocksForResourceProviders == true)
            {
                return services;
            }

            // Client factories
            services.AddSingleton<IBatchClientFactory, BatchClientFactory>();

            // Core services
            services.AddSingleton(storageProviderSettings);
            services.AddSingleton<IStorageProvider, StorageFileShareProvider>();
            services.AddSingleton<IStorageFileShareProviderHelper, StorageFileShareProviderHelper>();
            services.AddSingleton<IBatchPrepareFileShareJobProvider, BatchPrepareFileShareJobProvider>();
            services.AddSingleton<IBatchArchiveFileShareJobProvider, BatchArchiveFileShareJobProvider>();

            // Jobs
            services.AddSingleton<IAsyncBackgroundWarmup, StorageFileShareProviderRegisterJobs>();

            // Job Registration
            services.AddSingleton<IWatchStorageAzureBatchCleanupTask, WatchStorageAzureBatchCleanupTask>();

            return services;
        }
    }
}
