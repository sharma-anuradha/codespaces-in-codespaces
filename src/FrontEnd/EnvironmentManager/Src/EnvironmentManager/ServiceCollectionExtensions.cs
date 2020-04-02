// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor.RepairWorkflows;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common.Repositories.AzureQueue;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the Environment Manager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="CloudEnvironmentRepository"/> and <see cref="IEnvironmentManager"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="environmentManagerSettings">Target environment manager settings.</param>
        /// <param name="environmentMonitorSettings">Target environment monitor settings.</param>
        /// <param name="useMockCloudEnvironmentRepository">A value indicating whether to use a mock repository.</param>
        /// <param name="disableBackgroundTasks">A value indicating whether non-critical background tasks are disabled.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddEnvironmentManager(
            this IServiceCollection services,
            EnvironmentManagerSettings environmentManagerSettings,
            EnvironmentMonitorSettings environmentMonitorSettings,
            bool useMockCloudEnvironmentRepository,
            bool disableBackgroundTasks)
        {
            services.AddSingleton(environmentManagerSettings);
            services.AddSingleton(environmentMonitorSettings);

            if (useMockCloudEnvironmentRepository)
            {
                services.AddSingleton<ICloudEnvironmentRepository, MockCloudEnvironmentRepository>();
                services.AddSingleton<IContinuationJobQueueRepository, MockEnvironmentMonitorQueueRepository>();
            }
            else
            {
                services.AddVsoDocumentDbCollection<CloudEnvironment, ICloudEnvironmentRepository, DocumentDbCloudEnvironmentRepository>(DocumentDbCloudEnvironmentRepository.ConfigureOptions);
                services.AddSingleton<IContinuationJobQueueRepository, FrontendJobQueueRepository>();
                services.AddSingleton<ICrossRegionContinuationJobQueueRepository, CrossRegionFrontendJobQueueRepository>();
                services.AddSingleton<IStorageQueueClientProvider, StorageQueueClientProvider>();
                services.AddSingleton<ICrossRegionStorageQueueClientProvider, CrossRegionStorageQueueClientProvider>();
            }

            services.AddSingleton<IEnvironmentStateManager, EnvironmentStateManager>();

            // Continuation
            services.AddSingleton<IContinuationTaskWorkerPoolManager, ContinuationTaskWorkerPoolManager>();
            services.AddSingleton<IContinuationTaskMessagePump, ContinuationTaskMessagePump>();
            services.AddSingleton<IContinuationTaskWorkerPoolManager, ContinuationTaskWorkerPoolManager>();
            services.AddSingleton<IContinuationTaskActivator, ContinuationTaskActivator>();
            services.AddTransient<IContinuationTaskWorker, ContinuationTaskWorker>();

            services.AddSingleton<IEnvironmentMonitor, EnvironmentMonitor>();

            // Continuation - Cross region.
            services.AddSingleton<ICrossRegionControlPlaneInfo, CrossRegionControlPlaneInfo>();
            services.AddSingleton<ICrossRegionContinuationTaskMessagePump, CrossRegionContinuationTaskMessagePump>();
            services.AddSingleton<ICrossRegionContinuationTaskActivator, CrossRegionContinuationTaskActivator>();

            // Handlers
            services.AddSingleton<ILatestHeartbeatMonitor, LatestHeartbeatMonitor>();

            services.AddSingleton<ForceSuspendEnvironmentWorkflow>();
            services.AddSingleton<IForceSuspendEnvironmentWorkflow>(x => x.GetRequiredService<ForceSuspendEnvironmentWorkflow>());
            services.AddSingleton<IEnvironmentRepairWorkflow>(x => x.GetRequiredService<ForceSuspendEnvironmentWorkflow>());

            services.AddSingleton<InactiveEnvironmentWorkflow>();
            services.AddSingleton<IInactiveEnvironmentWorkflow>(x => x.GetRequiredService<InactiveEnvironmentWorkflow>());
            services.AddSingleton<IEnvironmentRepairWorkflow>(x => x.GetRequiredService<InactiveEnvironmentWorkflow>());

            services.AddSingleton<HeartbeatMonitorContinuationHandler>();
            services.AddSingleton<IHeartbeatMonitorContinuationHandler>(x => x.GetRequiredService<HeartbeatMonitorContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<HeartbeatMonitorContinuationHandler>());

            services.AddSingleton<EnvironmentStateTransitionMonitorContinuationHandler>();
            services.AddSingleton<IEnvironmentStateTransitionMonitorContinuationHandler>(x => x.GetRequiredService<EnvironmentStateTransitionMonitorContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<EnvironmentStateTransitionMonitorContinuationHandler>());

            services.AddSingleton<ArchiveEnvironmentContinuationHandler>();
            services.AddSingleton<IArchiveEnvironmentContinuationHandler>(x => x.GetRequiredService<ArchiveEnvironmentContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<ArchiveEnvironmentContinuationHandler>());

            services.AddSingleton<CreateEnvironmentContinuationHandler>();
            services.AddSingleton<ICreateEnvironmentContinuationHandler>(x => x.GetRequiredService<CreateEnvironmentContinuationHandler>());
            services.AddSingleton<IContinuationTaskMessageHandler>(x => x.GetRequiredService<CreateEnvironmentContinuationHandler>());

            // The environment mangaer
            services.AddSingleton<IEnvironmentManager, EnvironmentManager>();
            services.AddSingleton<IEnvironmentContinuationOperations, EnvironmentContinuationOperations>();

            if (!disableBackgroundTasks)
            {
                // Register background tasks
                services.AddSingleton<IWatchOrphanedSystemEnvironmentsTask, WatchOrphanedSystemEnvironmentsTask>();
                services.AddSingleton<IWatchFailedEnvironmentTask, WatchFailedEnvironmentTask>();
                services.AddSingleton<IWatchSuspendedEnvironmentsToBeArchivedTask, WatchSuspendedEnvironmentsToBeArchivedTask>();
                services.AddSingleton<ILogCloudEnvironmentStateTask, LogCloudEnvironmentStateTask>();
                services.AddSingleton<ILogSubscriptionStatisticsTask, LogSubscriptionStatisticsTask>();

                // Job warmup
                services.AddSingleton<IAsyncBackgroundWarmup, EnvironmentRegisterJobs>();
            }

            return services;
        }
    }
}
