﻿// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
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
            services.AddSingleton<IResourcePoolManager, ResourcePoolManager>();
            services.AddSingleton<ResourcePoolDefinitionStore>();
            services.AddSingleton<IResourceScalingHandler>(x => x.GetRequiredService<ResourcePoolDefinitionStore>());
            services.AddSingleton<IResourcePoolDefinitionStore>(x => x.GetRequiredService<ResourcePoolDefinitionStore>());
            services.AddSingleton<IResourceHeartBeatManager, ResourceHeartBeatManager>();
            services.AddSingleton<IResourceContinuationOperations, ResourceContinuationOperations>();

            // Continuation
            services.AddSingleton<IContinuationTaskWorkerPoolManager, ContinuationTaskWorkerPoolManager>();
            services.AddSingleton<IContinuationTaskMessagePump, ContinuationTaskMessagePump>();
            services.AddSingleton<IContinuationTaskWorkerPoolManager, ContinuationTaskWorkerPoolManager>();
            services.AddSingleton<IContinuationTaskActivator, ContinuationTaskActivator>();
            services.AddTransient<IContinuationTaskWorker, ContinuationTaskWorker>();

            // Azure
            services.AddSingleton<IAzureClientFactory, AzureClientFactory>();

            // Jobs
            services.AddSingleton<ResourceRegisterJobs>();
            services.AddSingleton<IAsyncBackgroundWarmup>(x => x.GetRequiredService<ResourceRegisterJobs>());

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
            services.AddSingleton<DeleteOrphanedResourceContinuationHandler>();
            services.AddSingleton<CleanupResourceContinuationHandler>();
            services.AddSingleton<ICleanupResourceContinuationHandler>(x => x.GetRequiredService<CleanupResourceContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<CleanupResourceContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<DeleteOrphanedResourceContinuationHandler>());

            // Job Registration
            services.AddSingleton(resourceBrokerSettings);
            services.AddSingleton<IDeleteResourceGroupDeploymentsTask, DeleteResourceGroupDeploymentsTask>();
            services.AddSingleton<IWatchPoolSizeTask, WatchPoolSizeTask>();
            services.AddSingleton<IWatchPoolVersionTask, WatchPoolVersionTask>();
            services.AddSingleton<IWatchPoolStateTask, WatchPoolStateTask>();
            services.AddSingleton<IWatchFailedResourcesTask, WatchFailedResourcesTask>();
            services.AddSingleton<IWatchOrphanedAzureResourceTask, WatchOrphanedAzureResourceTask>();
            services.AddSingleton<IWatchOrphanedSystemResourceTask, WatchOrphanedSystemResourceTask>();

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
                services.AddSingleton<IContinuationJobQueueRepository, MockResourceJobQueueRepository>();
                services.AddSingleton<IResourceRepository, MockResourceRepository>();

                return;
            }

            // Register Document Db Items
            services.AddDocumentDbCollection<ResourceRecord, IResourceRepository, CosmosDbResourceRepository>(
                CosmosDbResourceRepository.ConfigureOptions);
            services.AddDocumentDbCollection<ResourcePoolStateSnapshotRecord, IResourcePoolStateSnapshotRepository, CosmosDbResourcePoolStateSnapshotRepository>(
                CosmosDbResourcePoolStateSnapshotRepository.ConfigureOptions);

            // Register Queue Items
            services.AddSingleton<IContinuationJobQueueRepository, StorageResourceJobQueueRepository>();

            // SDK provider
            services.AddSingleton<IStorageQueueClientProvider, StorageQueueClientProvider>();
        }
    }
}
