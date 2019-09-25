// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureCosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the system catalog.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// <see cref="IServiceCollection"/> extensions for the resource broker.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="resourceBrokerSettings">The resource broker settings.</param>
        /// <param name="mocksSettings">The mocks settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddResourceBroker(
            this IServiceCollection services,
            ResourceBrokerSettings resourceBrokerSettings,
            MocksSettings mocksSettings = null)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(resourceBrokerSettings, nameof(resourceBrokerSettings));

            // Short circuit things if Resource Broker is being mocked
            if (mocksSettings?.UseMocksForResourceBroker == true)
            {
                services.AddSingleton<IResourceBroker, MockResourceBroker>();
                return services;
            }

            // Core services
            services.AddSingleton<IResourceBroker, ResourceBroker>();
            services.AddSingleton<ResourcePoolManager>();
            services.AddSingleton<IResourcePoolManager>(x => x.GetRequiredService<ResourcePoolManager>());
            services.AddSingleton<IResourcePoolSettingsHandler>(x => x.GetRequiredService<ResourcePoolManager>());
            services.AddSingleton<ResourcePoolDefinitionStore>();
            services.AddSingleton<IResourceScalingHandler>(x => x.GetRequiredService<ResourcePoolDefinitionStore>());
            services.AddSingleton<IResourcePoolDefinitionStore>(x => x.GetRequiredService<ResourcePoolDefinitionStore>());

            // Continuation
            services.AddSingleton<IContinuationTaskWorkerPoolManager, ContinuationTaskWorkerPoolManager>();
            services.AddSingleton<IContinuationTaskMessagePump, ContinuationTaskMessagePump>();
            services.AddSingleton<IContinuationTaskWorkerPoolManager, ContinuationTaskWorkerPoolManager>();
            services.AddSingleton<IContinuationTaskActivator, ContinuationTaskActivator>();
            services.AddTransient<IContinuationTaskWorker, ContinuationTaskWorker>();

            // Jobs
            services.AddSingleton<IAsyncBackgroundWarmup, ResourceRegisterJobs>();

            // Handlers
            services.AddSingleton<CreateResourceContinuationHandler>();
            services.AddSingleton<ICreateResourceContinuationHandler>(x => x.GetRequiredService<CreateResourceContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<CreateResourceContinuationHandler>());
            services.AddSingleton<StartEnvironmentContinuationHandler>();
            services.AddSingleton<IStartEnvironmentContinuationHandler>(x => x.GetRequiredService<StartEnvironmentContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<StartEnvironmentContinuationHandler>());
            services.AddSingleton<DeleteResourceContinuationHandler>();
            services.AddSingleton<IDeleteResourceContinuationHandler>(x => x.GetRequiredService<DeleteResourceContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<DeleteResourceContinuationHandler>());

            // Job Registration
            services.AddSingleton(resourceBrokerSettings);
            services.AddSingleton<IWatchPoolSizeTask, WatchPoolSizeTask>();
            services.AddSingleton<IWatchPoolVersionTask, WatchPoolVersionTask>();
            services.AddSingleton<IWatchPoolStateTask, WatchPoolStateTask>();
            services.AddSingleton<IWatchPoolSettingsTask, WatchPoolSettingsTask>();

            if (mocksSettings?.UseMocksForResourceProviders == true)
            {
                services.AddSingleton<IComputeProvider, MockComputeProvider>();
                services.AddSingleton<IStorageProvider, MockStorageProvider>();
            }

            ConfigureDataServices(services, mocksSettings);
            return services;
        }

        private static void ConfigureDataServices(
            IServiceCollection services,
            MocksSettings mocksSettings = null)
        {
            // Use the mock services if we're developing locally
            if (mocksSettings?.UseMocksForExternalDependencies == true)
            {
                services.AddSingleton<IResourceJobQueueRepository, MockResourceJobQueueRepository>();
                services.AddSingleton<IResourceRepository, MockResourceRepository>();

                return;
            }

            // Register Document Db Items
            services.AddDocumentDbCollection<ResourceRecord, IResourceRepository, CosmosDbResourceRepository>(
                CosmosDbResourceRepository.ConfigureOptions);
            services.AddDocumentDbCollection<ResourcePoolStateSnapshotRecord, IResourcePoolStateSnapshotRepository, CosmosDbResourcePoolStateSnapshotRepository>(
                CosmosDbResourceRepository.ConfigureOptions);
            services.AddDocumentDbCollection<ResourcePoolSettingsRecord, IResourcePoolSettingsRepository, CosmosDbResourcePoolSettingsRepository>(
                CosmosDbResourceRepository.ConfigureOptions);

            // Register Queue Items
            services.AddSingleton<IResourceJobQueueRepository, StorageResourceJobQueueRepository>();

            // SDK provider
            services.AddSingleton<IStorageQueueClientProvider, StorageQueueClientProvider>();
        }
    }
}
