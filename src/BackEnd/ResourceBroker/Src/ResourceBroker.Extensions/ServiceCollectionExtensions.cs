// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureCosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureQueue;
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
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="storageAccountSettings"></param>
        /// <param name="resourceBrokerSettings"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        public static IServiceCollection AddResourceBroker(
            this IServiceCollection services,
            StorageAccountSettings storageAccountSettings,
            ResourceBrokerSettings resourceBrokerSettings,
            AppSettings appSettings)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(storageAccountSettings, nameof(storageAccountSettings));
            Requires.NotNull(resourceBrokerSettings, nameof(resourceBrokerSettings));
            Requires.NotNull(appSettings, nameof(appSettings));

            // Short circuit things if Resource Broker is being mocked
            if (appSettings.UseMocksForResourceBroker)
            {
                services.AddSingleton<IResourceBroker, MockResourceBroker>();
                return services;
            }

            // Core services
            services.AddSingleton<IResourceBroker, ResourceBroker>();
            services.AddSingleton<IResourcePool, ResourcePool>();
            services.AddSingleton<ResourceScalingBroker>();
            services.AddSingleton<IResourceScalingBroker>(x => x.GetRequiredService<ResourceScalingBroker>());
            services.AddSingleton<IResourceScalingStore>(x => x.GetRequiredService<ResourceScalingBroker>());

            // Continuation
            services.AddSingleton<IContinuationTaskWorkerPoolManager, ContinuationTaskWorkerPoolManager>();
            services.AddSingleton<IContinuationTaskMessagePump, ContinuationTaskMessagePump>();
            services.AddSingleton<IContinuationTaskWorkerPoolManager, ContinuationTaskWorkerPoolManager>();
            services.AddSingleton<IContinuationTaskActivator, ContinuationTaskActivator>();
            services.AddTransient<IContinuationTaskWorker, ContinuationTaskWorker>();

            // Jobs
            services.AddSingleton<IAsyncBackgroundWarmup, ResourceRegisterJobs>();

            // Tasks
            services.AddSingleton<CreateComputeContinuationHandler>();
            services.AddSingleton<ICreateComputeContinuationHandler>(x => x.GetRequiredService<CreateComputeContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<CreateComputeContinuationHandler>());
            services.AddSingleton<CreateStorageContinuationHandler>();
            services.AddSingleton<ICreateStorageContinuationHandler>(x => x.GetRequiredService<CreateStorageContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<CreateStorageContinuationHandler>());
            services.AddSingleton<StartEnvironmentContinuationHandler>();
            services.AddSingleton<IStartEnvironmentContinuationHandler>(x => x.GetRequiredService<StartEnvironmentContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<StartEnvironmentContinuationHandler>());

            // Job Registration
            services.AddSingleton<IWatchPoolSizeTask, WatchPoolSizeTask>();

            if (appSettings.UseMocksForResourceProviders)
            {
                services.AddSingleton<IComputeProvider, MockComputeProvider>();
                services.AddSingleton<IStorageProvider, MockStorageProvider>();
            }

            ConfigureDataServices(services, appSettings);
            ConfigureQueue(services, appSettings);

            return services;
        }

        private static void ConfigureDataServices(
            IServiceCollection services,
            AppSettings appSettings)
        {
            // Use the mock services if we're developing locally
            if (appSettings.UseMocksForExternalDependencies)
            {
                services.AddSingleton<IResourceJobQueueRepository, MockResourceJobQueueRepository>();
                services.AddSingleton<IResourceRepository, MockResourceRepository>();

                return;
            }

            // Register Document Db Items
            services.AddDocumentDbCollection<ResourceRecord, IResourceRepository, CosmosDbResourceRepository>(
                CosmosDbResourceRepository.ConfigureOptions);

            // Register Queue Items
            services.AddSingleton<IResourceJobQueueRepository, QueueResourceJobQueueRepository>();

            // SDK provider
            services.AddSingleton<IStorageQueueClientProvider, StorageQueueClientProvider>();
        }

        private static void ConfigureQueue(IServiceCollection services, AppSettings appSettings)
        {
            // Add Hangfire services.
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMemoryStorage());

            // Add the processing server as IHostedService
            services.AddHangfireServer(configuration => configuration
                .Queues = new string[]
                    {
                        MockResourceJobQueueRepository.QueueName,
                        "background-warmup-job-queue", // TODO: Need to fix this reference somehow
                    });
        }
    }
}
