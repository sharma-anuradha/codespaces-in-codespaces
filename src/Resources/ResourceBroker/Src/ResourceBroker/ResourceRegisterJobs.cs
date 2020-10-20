// <copyright file="ResourceRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Registeres any jobs that need to be run on warmup.
    /// </summary>
    public class ResourceRegisterJobs : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRegisterJobs"/> class.
        /// </summary>
        /// <param name="deleteResourceGroupDeploymentsTask">Task to delete resource group deployments.</param>
        /// <param name="jobSchedulersRegisters">List of job scheduler to register.</param>
        /// <param name="watchOrphanedPoolTask">Target watch orphaned pool job.</param>
        /// <param name="watchOrphanedAzureResourceTask">Target watch orphaned Azure resources job.</param>
        /// <param name="watchOrphanedVmAgentImagesTask">Target watch orphaned VM images/blobs job.</param>
        /// <param name="watchOrphanedStorageImagesTask">Target watch orphaned storage images/blobs job.</param>
        /// <param name="watchOrphanedComputeImagesTask">Target watch orphaned compute images job.</param>
        /// <param name="continuationTaskMessagePump">Target Continuation Task Message Pump.</param>
        /// <param name="continuationTaskWorkerPoolManager">Target Continuation Task Worker Pool Manager.</param>
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        /// <param name="jobQueueConsumerFactory">The job consumer factory instance.</param>
        /// <param name="jobHandlers">All the job handlers.</param>
        /// <param name="jobHandlerTargets">All the job handler targets.</param>
        /// <param name="systemConfiguration">The system configuration.</param>
        /// <param name="watchPoolSizeJob">Target watch pool size job.</param>
        /// <param name="watchPoolVersionTask">Target watch pool version job interface.</param>
        /// <param name="watchPoolStateTask">Target watch pool state task.</param>
        /// <param name="watchFailedResourcesTask">Target watch failed resources job.</param>
        /// <param name="refreshKeyVaultSecretCacheTask">Refresh key vault secret cache task.</param>
        public ResourceRegisterJobs(
            IDeleteResourceGroupDeploymentsTask deleteResourceGroupDeploymentsTask,
            IEnumerable<IJobSchedulerRegister> jobSchedulersRegisters,
            IWatchOrphanedPoolTask watchOrphanedPoolTask,
            IWatchOrphanedAzureResourceTask watchOrphanedAzureResourceTask,
            WatchOrphanedVmAgentImagesTask watchOrphanedVmAgentImagesTask,
            WatchOrphanedStorageImagesTask watchOrphanedStorageImagesTask,
            WatchOrphanedComputeImagesTask watchOrphanedComputeImagesTask,
            IContinuationTaskMessagePump continuationTaskMessagePump,
            IContinuationTaskWorkerPoolManager continuationTaskWorkerPoolManager,
            ITaskHelper taskHelper,
            IJobQueueConsumerFactory jobQueueConsumerFactory,
            IEnumerable<IJobHandler> jobHandlers,
            IEnumerable<IJobHandlerTarget> jobHandlerTargets,
            ISystemConfiguration systemConfiguration,
            IWatchPoolSizeTask watchPoolSizeJob,
            IWatchPoolVersionTask watchPoolVersionTask,
            IWatchPoolStateTask watchPoolStateTask,
            IWatchFailedResourcesTask watchFailedResourcesTask,
            IRefreshKeyVaultSecretCacheTask refreshKeyVaultSecretCacheTask)
        {
            DeleteResourceGroupDeploymentsTask = deleteResourceGroupDeploymentsTask;
            JobSchedulersRegisters = jobSchedulersRegisters;
            WatchOrphanedPoolTask = watchOrphanedPoolTask;
            WatchOrphanedAzureResourceTask = watchOrphanedAzureResourceTask;
            WatchOrphanedVmAgentImagesTask = watchOrphanedVmAgentImagesTask;
            WatchOrphanedStorageImagesTask = watchOrphanedStorageImagesTask;
            WatchOrphanedComputeImagesTask = watchOrphanedComputeImagesTask;
            ContinuationTaskMessagePump = continuationTaskMessagePump;
            ContinuationTaskWorkerPoolManager = continuationTaskWorkerPoolManager;
            TaskHelper = taskHelper;
            JobQueueConsumerFactory = jobQueueConsumerFactory;
            JobHandlers = jobHandlers;
            JobHandlerTargets = jobHandlerTargets;
            SystemConfiguration = systemConfiguration;
            Random = new Random();

            WatchPoolSizeJob = watchPoolSizeJob;
            WatchPoolVersionTask = watchPoolVersionTask;
            WatchOrphanedPoolTask = watchOrphanedPoolTask;
            WatchPoolStateTask = watchPoolStateTask;
            WatchFailedResourcesTask = watchFailedResourcesTask;
            RefreshKeyVaultSecretCacheTask = refreshKeyVaultSecretCacheTask;
        }

        private IEnumerable<IJobSchedulerRegister> JobSchedulersRegisters { get; }

        private IWatchOrphanedPoolTask WatchOrphanedPoolTask { get; }

        private WatchOrphanedVmAgentImagesTask WatchOrphanedVmAgentImagesTask { get; }

        private WatchOrphanedStorageImagesTask WatchOrphanedStorageImagesTask { get; }

        private WatchOrphanedComputeImagesTask WatchOrphanedComputeImagesTask { get; }

        private IContinuationTaskMessagePump ContinuationTaskMessagePump { get; }

        private IContinuationTaskWorkerPoolManager ContinuationTaskWorkerPoolManager { get; }

        private ITaskHelper TaskHelper { get; }

        private IJobQueueConsumerFactory JobQueueConsumerFactory { get; }

        private IEnumerable<IJobHandler> JobHandlers { get; }

        private IEnumerable<IJobHandlerTarget> JobHandlerTargets { get; }

        private ISystemConfiguration SystemConfiguration { get; }

        private IRefreshKeyVaultSecretCacheTask RefreshKeyVaultSecretCacheTask { get; }

        private Random Random { get; }

        // Note: deprecated
        private IWatchPoolSizeTask WatchPoolSizeJob { get; }

        private IWatchPoolVersionTask WatchPoolVersionTask { get; }

        private IWatchPoolStateTask WatchPoolStateTask { get; }

        private IWatchFailedResourcesTask WatchFailedResourcesTask { get; }

        private IWatchOrphanedAzureResourceTask WatchOrphanedAzureResourceTask { get; }

        private IDeleteResourceGroupDeploymentsTask DeleteResourceGroupDeploymentsTask { get; }

        /// <inheritdoc/>
        public async Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            // Job: Continuation Task Worker Pool Manager
            TaskHelper.RunBackground(
                $"{ResourceLoggingConstants.ContinuationTaskWorkerPoolManager}_start",
                (childLogger) => ContinuationTaskWorkerPoolManager.StartAsync(childLogger),
                autoLogOperation: false);

            // Job: Populate continuation message cache
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.ContinuationTaskMessagePump}_run_try_populate_cache",
                (childLogger) => ContinuationTaskMessagePump.RunTryPopulateCacheAsync(childLogger),
                TimeSpan.FromSeconds(10));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Register job handlers
            JobQueueConsumerFactory
                .GetOrCreate(ResourceJobQueueConstants.GenericQueueName)
                .RegisterJobHandlers(JobHandlers)
                .Start(QueueMessageProducerSettings.Default);

            // new job continuation handlers
            JobQueueConsumerFactory.RegisterJobHandlers(JobHandlerTargets);
            JobQueueConsumerFactory.Start(JobHandlerTargets, QueueMessageProducerSettings.Default);

            // register all the job schedulers
            foreach (var jobSchedulersRegister in JobSchedulersRegisters)
            {
                jobSchedulersRegister.RegisterScheduleJob();
            }

            // Note: this next section will be eventually deprecated and removed
#if true
            // Job: Watch Pool Size
            var watchPoolSizeTaskTimeSpan = TimeSpan.FromMinutes(1);
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolSizeTask}_run",
                (childLogger) => WatchPoolSizeJob.RunTaskAsync(watchPoolSizeTaskTimeSpan, childLogger),
                watchPoolSizeTaskTimeSpan);

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Pool Version
            var watchPoolVersionTaskSpan = TimeSpan.FromMinutes(1);
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolVersionTask}_run",
                (childLogger) => WatchPoolVersionTask.RunTaskAsync(watchPoolVersionTaskSpan, childLogger),
                watchPoolVersionTaskSpan);

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Pool State
            var watchPoolStateTaskSpan = TimeSpan.FromMinutes(1);
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolStateTask}_run",
                (childLogger) => WatchPoolStateTask.RunTaskAsync(watchPoolStateTaskSpan, childLogger),
                watchPoolStateTaskSpan);

            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Failed Resources
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchFailedResourcesTask}_run",
                (childLogger) => WatchFailedResourcesTask.RunTaskAsync(TimeSpan.FromMinutes(30), childLogger),
                TimeSpan.FromMinutes(5));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Orphaned Azure Resources
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchOrphanedAzureResourceTask}_run",
                (childLogger) => WatchOrphanedAzureResourceTask.RunTaskAsync(TimeSpan.FromMinutes(30), childLogger),
                TimeSpan.FromMinutes(10));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Delete Resource Group Deployments
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.DeleteResourceGroupDeploymentsTask}_run",
                (childLogger) => DeleteResourceGroupDeploymentsTask.RunTaskAsync(TimeSpan.FromHours(1), childLogger),
                TimeSpan.FromMinutes(10));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));
#endif

            // Job: Delete Artifact images
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchOrphanedVmAgentImagesTask}_run",
                (childLogger) => WatchOrphanedVmAgentImagesTask.RunTaskAsync(TimeSpan.FromDays(1), childLogger),
                TimeSpan.FromHours(1));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Delete Artifact storage images
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchOrphanedStorageImagesTask}_run",
                (childLogger) => WatchOrphanedStorageImagesTask.RunTaskAsync(TimeSpan.FromDays(1), childLogger),
                TimeSpan.FromHours(1));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Delete Artifact Nexus windows images
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchOrphanedComputeImagesTask}_run",
                (childLogger) => WatchOrphanedComputeImagesTask.RunTaskAsync(TimeSpan.FromDays(1), childLogger),
                TimeSpan.FromHours(1));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Delete orphaned pools.
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchOrphanedPoolTask}_run",
                (childLogger) => WatchOrphanedPoolTask.RunTaskAsync(TimeSpan.FromDays(1), childLogger),
                TimeSpan.FromHours(1));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Refresh Key Vault Secret Cache Task
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.RefreshKeyVaultSecretCacheTask}_run",
                (childLogger) => RefreshKeyVaultSecretCacheTask.RunTaskAsync(TimeSpan.FromMinutes(10), childLogger),
                TimeSpan.FromHours(4));
        }
    }
}
