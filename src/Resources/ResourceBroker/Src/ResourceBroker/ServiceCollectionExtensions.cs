// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Strategies;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureCosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Strategies;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
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

            // Resource broker strategies
            services.AddSingleton<IAllocationStrategy, AllocationBasicStrategy>();
            services.AddSingleton<IAllocationStrategy, AllocationOSDiskResumeStrategy>();
            services.AddSingleton<IAllocationStrategy, AllocationOSDiskCreateStrategy>();
            services.AddSingleton<IAllocationStrategy, AllocationOSDiskSnapshotStrategy>();

            // Continuation
            services.AddSingleton<IContinuationTaskWorkerPoolManager, ContinuationTaskWorkerPoolManager>();
            services.AddSingleton<IContinuationTaskMessagePump, ContinuationTaskMessagePump>();
            services.AddSingleton<IContinuationTaskWorkerPoolManager, ContinuationTaskWorkerPoolManager>();
            services.AddSingleton<IContinuationTaskActivator, ContinuationTaskActivator>();
            services.AddTransient<IContinuationTaskWorker, ContinuationTaskWorker>();

            // Job handlers
            services.AddSingleton<IJobHandler, WatchPoolVersionJobHandler>();
            services.AddSingleton<IJobHandler, WatchFailedResourcesJobHandler>();
            services.AddSingleton<IJobHandler, WatchPoolSizeJobHandler>();
            services.AddSingleton<IJobHandler, WatchPoolStateJobHandler>();
            services.AddSingleton<IJobHandler, WatchOrphanedVmAgentImagesJobHandler>();
            services.AddSingleton<IJobHandler, WatchOrphanedStorageImagesJobHandler>();

            // Jobs
            services.AddSingleton<ResourceRegisterJobs>();
            services.AddSingleton<IAsyncBackgroundWarmup>(x => x.GetRequiredService<ResourceRegisterJobs>());

            // Handlers
            services.AddSingleton<CreateResourceContinuationHandlerV2>();
            services.AddSingleton<ICreateResourceContinuationHandler>(x => x.GetRequiredService<CreateResourceContinuationHandlerV2>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<CreateResourceContinuationHandlerV2>());
            services.AddSingleton<ResumeEnvironmentContinuationHandler>();
            services.AddSingleton<IStartEnvironmentContinuationHandler>(x => x.GetRequiredService<ResumeEnvironmentContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<ResumeEnvironmentContinuationHandler>());
            services.AddSingleton<ExportEnvironmentContinuationHandler>();
            services.AddSingleton<IExportEnvironmentContinuationHandler>(x => x.GetRequiredService<ExportEnvironmentContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<ExportEnvironmentContinuationHandler>());
            services.AddSingleton<StartArchiveContinuationHandler>();
            services.AddSingleton<IStartArchiveContinuationHandler>(x => x.GetRequiredService<StartArchiveContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<StartArchiveContinuationHandler>());
            services.AddSingleton<DeleteResourceContinuationHandler>();
            services.AddSingleton<IDeleteResourceContinuationHandler>(x => x.GetRequiredService<DeleteResourceContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<DeleteResourceContinuationHandler>());
            services.AddSingleton<CleanupResourceContinuationHandler>();
            services.AddSingleton<ICleanupResourceContinuationHandler>(x => x.GetRequiredService<CleanupResourceContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<CleanupResourceContinuationHandler>());

            // Create resource strategies
            services.AddSingleton<ICreateResourceStrategy, CreateStorageFileShareStrategy>();
            services.AddSingleton<ICreateResourceStrategy, CreateKeyVaultStrategy>();
            services.AddSingleton<ICreateResourceStrategy, CreateComputeWithComponentsStrategy>();
            services.AddSingleton<ICreateComponentStrategy, CreateComputeStrategy>();
            services.AddSingleton<ICreateComponentStrategy, CreateNetworkInterfaceStrategy>();
            services.AddSingleton<ICreateComponentStrategy, CreateQueueStrategy>();

            // Resource Request Manager
            services.AddSingleton<IResourceStateManager, ResourceStateManager>();
            services.AddSingleton<IResourceRequestManager, ResourceRequestManager>();
            services.AddSingleton<IResourceRequestQueueProvider, ResourceRequestQueueProvider>();

            // Job payload factories
            services.AddSingleton<WatchPoolPayloadFactory>();

            // Job schedule Registration
            services.AddSingleton<IJobSchedulerRegister, WatchPoolJobScheduleRegister>();
            services.AddSingleton<IJobSchedulerRegister, WatchOrphanedStorageImagesProducer>();
            services.AddSingleton<IJobSchedulerRegister, WatchOrphanedVmAgentImagesProducer>();

            // Job Registration
            services.AddSingleton(resourceBrokerSettings);
            services.AddSingleton<IDeleteResourceGroupDeploymentsTask, DeleteResourceGroupDeploymentsTask>();
            services.AddSingleton<IWatchOrphanedPoolTask, WatchOrphanedPoolTask>();
            services.AddSingleton<IWatchOrphanedAzureResourceTask, WatchOrphanedAzureResourceTask>();
            services.AddSingleton<IWatchOrphanedSystemResourceTask, WatchOrphanedSystemResourceTask>();
            services.AddSingleton<WatchOrphanedComputeImagesTask>();
            services.AddSingleton<IRefreshKeyVaultSecretCacheTask, RefreshKeyVaultSecretCacheTask>();
            services.AddSingleton<ILogSystemResourceStateTask, LogSystemResourceStateTask>();

            // deprecated job handlers
            services.AddSingleton<IWatchPoolSizeTask, WatchPoolSizeTask>();
            services.AddSingleton<IWatchPoolVersionTask, WatchPoolVersionTask>();
            services.AddSingleton<IWatchPoolStateTask, WatchPoolStateTask>();
            services.AddSingleton<IWatchFailedResourcesTask, WatchFailedResourcesTask>();
            services.AddSingleton<WatchOrphanedVmAgentImagesTask>();
            services.AddSingleton<WatchOrphanedStorageImagesTask>();

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
            services.AddVsoDocumentDbCollection<ResourceRecord, IResourceRepository, CosmosDbResourceRepository>(
                CosmosDbResourceRepository.ConfigureOptions);
            services.AddVsoDocumentDbCollection<ResourcePoolStateSnapshotRecord, IResourcePoolStateSnapshotRepository, CosmosDbResourcePoolStateSnapshotRepository>(
                CosmosDbResourcePoolStateSnapshotRepository.ConfigureOptions);

            // Register Queue Items
            services.AddSingleton<IContinuationJobQueueRepository, StorageResourceJobQueueRepository>();

            // SDK provider
            services.AddSingleton<IStorageQueueClientProvider, StorageQueueClientProvider>();
            services.AddSingleton<ICrossRegionStorageQueueClientProvider, CrossRegionStorageQueueClientProvider>();
            services.AddSingleton<ICrossRegionControlPlaneInfo, CrossRegionControlPlaneInfo>();
        }
    }
}
