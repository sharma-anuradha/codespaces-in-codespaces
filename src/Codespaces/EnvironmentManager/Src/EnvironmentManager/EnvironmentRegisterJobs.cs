// <copyright file="EnvironmentRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment Register Jobs.
    /// </summary>
    public class EnvironmentRegisterJobs : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentRegisterJobs"/> class.
        /// </summary>
        /// <param name="watchOrphanedSystemEnvironmentsTask">Target watch orphaned system environments task.</param>
        /// <param name="watchFailedEnvironmentTask">Target watch failed environment task.</param>
        /// <param name="watchSuspendedEnvironmentsToBeArchivedTask">Target watch suspended environments to be archived task.</param>
        /// <param name="logCloudEnvironmentStateTask">Target Log Cloud Environment State task.</param>
        /// <param name="logSubscriptionStatisticsTask">Target Log Subscriptions Statistics task.</param>
        /// <param name="cleanDeletedPlanEnvironmentsTask">Clean Deleted Environments task.</param>
        /// <param name="continuationTaskMessagePump">Target Continuation Task Message Pump.</param>
        /// <param name="continuationTaskWorkerPoolManager">Target Continuation Task Worker Pool Manager.</param>
        /// <param name="watchSoftDeletedEnvironmentToBeDeletedTask"> Target watch soft deleted environments to be terminated task.</param>
        /// <param name="watchEnvironmentsToBeUpdatedTask">Target watch for environments to be updated.</param>
        /// <param name="watchDeletedPlanSecretStoresTask">Target watch secrets stores to be deleted.</param>
        /// <param name="refreshKeyVaultSecretCacheTask">Refresh key vault secret cache task.</param>
        /// <param name="cloudEnvironmentRegionalMigrationTask">Env regional migration task.</param>
        /// <param name="syncRegionalEnvironmentsToGlobalTask">Sync task for cloud environments.</param>
        /// <param name="jobQueueConsumerFactory">Target Job Queue Consumer Factory.</param>
        /// <param name="watchExportBlobsTask">Target watch export blobs task.</param>
        /// <param name="jobHandlerTargets">All the job handlers.</param>
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        public EnvironmentRegisterJobs(
            IWatchOrphanedSystemEnvironmentsTask watchOrphanedSystemEnvironmentsTask,
            IWatchFailedEnvironmentTask watchFailedEnvironmentTask,
            IWatchEnvironmentsToBeArchivedTask watchSuspendedEnvironmentsToBeArchivedTask,
            ILogCloudEnvironmentStateTask logCloudEnvironmentStateTask,
            ILogSubscriptionStatisticsTask logSubscriptionStatisticsTask,
            IWatchDeletedPlanEnvironmentsTask cleanDeletedPlanEnvironmentsTask,
            IWatchDeletedPlanSecretStoresTask watchDeletedPlanSecretStoresTask,
            IContinuationTaskMessagePump continuationTaskMessagePump,
            IContinuationTaskWorkerPoolManager continuationTaskWorkerPoolManager,
            IWatchSoftDeletedEnvironmentToBeHardDeletedTask watchSoftDeletedEnvironmentToBeDeletedTask,
            IWatchEnvironmentsToBeUpdatedTask watchEnvironmentsToBeUpdatedTask,
            IRefreshKeyVaultSecretCacheTask refreshKeyVaultSecretCacheTask,
            ICloudEnvironmentRegionalMigrationTask cloudEnvironmentRegionalMigrationTask,
            ISyncRegionalEnvironmentsToGlobalTask syncRegionalEnvironmentsToGlobalTask,
            IJobQueueConsumerFactory jobQueueConsumerFactory,
            IWatchExportBlobsTask watchExportBlobsTask,
            IEnumerable<IJobHandlerTarget> jobHandlerTargets,
            ITaskHelper taskHelper)
        {
            WatchOrphanedSystemEnvironmentsTask = watchOrphanedSystemEnvironmentsTask;
            WatchFailedEnvironmentTask = watchFailedEnvironmentTask;
            WatchSuspendedEnvironmentsToBeArchivedTask = watchSuspendedEnvironmentsToBeArchivedTask;
            LogCloudEnvironmentStateTask = logCloudEnvironmentStateTask;
            LogSubscriptionStatisticsTask = logSubscriptionStatisticsTask;
            CleanDeletedPlanEnvironmentsTask = cleanDeletedPlanEnvironmentsTask;
            WatchDeletedPlanSecretStoresTask = watchDeletedPlanSecretStoresTask;
            ContinuationTaskMessagePump = continuationTaskMessagePump;
            ContinuationTaskWorkerPoolManager = continuationTaskWorkerPoolManager;
            WatchSoftDeletedEnvironmentToBeHardDeletedTask = watchSoftDeletedEnvironmentToBeDeletedTask;
            WatchEnvironmentsToBeUpdatedTask = watchEnvironmentsToBeUpdatedTask;
            RefreshKeyVaultSecretCacheTask = refreshKeyVaultSecretCacheTask;
            CloudEnvironmentRegionalMigrationTask = cloudEnvironmentRegionalMigrationTask;
            SyncRegionalEnvironmentsToGlobalTask = syncRegionalEnvironmentsToGlobalTask;
            JobHandlerTargets = jobHandlerTargets;
            JobQueueConsumerFactory = jobQueueConsumerFactory;
            WatchExportBlobsTask = watchExportBlobsTask;
            TaskHelper = taskHelper;
            Random = new Random();
        }

        private IWatchOrphanedSystemEnvironmentsTask WatchOrphanedSystemEnvironmentsTask { get; }

        private IWatchFailedEnvironmentTask WatchFailedEnvironmentTask { get; }

        private IWatchEnvironmentsToBeArchivedTask WatchSuspendedEnvironmentsToBeArchivedTask { get; }

        private ILogCloudEnvironmentStateTask LogCloudEnvironmentStateTask { get; }

        private ILogSubscriptionStatisticsTask LogSubscriptionStatisticsTask { get; }

        private IWatchDeletedPlanEnvironmentsTask CleanDeletedPlanEnvironmentsTask { get; }

        private IWatchDeletedPlanSecretStoresTask WatchDeletedPlanSecretStoresTask { get; }

        private IContinuationTaskMessagePump ContinuationTaskMessagePump { get; }

        private IContinuationTaskWorkerPoolManager ContinuationTaskWorkerPoolManager { get; }

        private IWatchSoftDeletedEnvironmentToBeHardDeletedTask WatchSoftDeletedEnvironmentToBeHardDeletedTask { get; }

        private IWatchEnvironmentsToBeUpdatedTask WatchEnvironmentsToBeUpdatedTask { get; }

        private IRefreshKeyVaultSecretCacheTask RefreshKeyVaultSecretCacheTask { get; }

        private ICloudEnvironmentRegionalMigrationTask CloudEnvironmentRegionalMigrationTask { get; }

        private ISyncRegionalEnvironmentsToGlobalTask SyncRegionalEnvironmentsToGlobalTask { get; }

        private IJobQueueConsumerFactory JobQueueConsumerFactory { get; }

        private IEnumerable<IJobHandlerTarget> JobHandlerTargets { get; }

        private IWatchExportBlobsTask WatchExportBlobsTask { get; }

        private ITaskHelper TaskHelper { get; }

        private Random Random { get; }

        /// <inheritdoc/>
        public async Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            // Job: Continuation Task Worker Pool Manager
            TaskHelper.RunBackground(
                $"{EnvironmentLoggingConstants.ContinuationTaskWorkerPoolManager}_start",
                (childLogger) => ContinuationTaskWorkerPoolManager.StartAsync(childLogger),
                autoLogOperation: false);

            // Job: Populate continuation message cache
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.ContinuationTaskMessagePump}_run_try_populate_cache",
                (childLogger) => ContinuationTaskMessagePump.RunTryPopulateCacheAsync(childLogger),
                TimeSpan.FromSeconds(10));

            // Job: Watch Orphaned System Resources
            TaskHelper.RunBackgroundLoop(
            $"{EnvironmentLoggingConstants.WatchOrphanedSystemEnvironmentsTask}_run",
            (childLogger) => WatchOrphanedSystemEnvironmentsTask.RunTaskAsync(TimeSpan.FromHours(1), childLogger),
            TimeSpan.FromMinutes(10));

            // Job: Migrate cloud environments to the appropriate regional database
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.CloudEnvironmentRegionalMigrationTask}_run",
                (childLogger) => CloudEnvironmentRegionalMigrationTask.RunTaskAsync(TimeSpan.FromHours(24), childLogger),
                TimeSpan.FromMinutes(10));

            // Job: Re-sync broken global environments with their regional records
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.SyncRegionalEnvironmentsToGlobalTask}_run",
                (childLogger) => SyncRegionalEnvironmentsToGlobalTask.RunTaskAsync(TimeSpan.FromHours(24), childLogger),
                TimeSpan.FromMinutes(10));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Job: Watch Suspended Environments to be Archived
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.WatchSuspendedEnvironmentsToBeArchivedTask}_run",
                (childLogger) => WatchSuspendedEnvironmentsToBeArchivedTask.RunTaskAsync(TimeSpan.FromMinutes(10), childLogger),
                TimeSpan.FromMinutes(2));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Job: Watch Failed Environment
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.WatchFailedEnvironmentTask}_run",
                (childLogger) => WatchFailedEnvironmentTask.RunTaskAsync(TimeSpan.FromMinutes(10), childLogger),
                TimeSpan.FromMinutes(2));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Job: Log Cloud Environment State
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.LogCloudEnvironmentsStateTask}_run",
                (childLogger) => LogCloudEnvironmentStateTask.RunTaskAsync(TimeSpan.FromMinutes(10), childLogger),
                TimeSpan.FromMinutes(1));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Job: Log Plan and Subscription Information
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.LogSubscriptionStatisticsTask}_run",
                (childLogger) => LogSubscriptionStatisticsTask.RunTaskAsync(TimeSpan.FromHours(1), childLogger),
                TimeSpan.FromMinutes(10));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Job: Refresh Key Vault Secret Cache Task
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.RefreshKeyVaultSecretCacheTask}_run",
                (childLogger) => RefreshKeyVaultSecretCacheTask.RunTaskAsync(TimeSpan.FromMinutes(10), childLogger),
                TimeSpan.FromHours(4));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Job: Delete Environments in deleted plans
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.WatchDeletedPlanEnvironmentsTask}_run",
                (childLogger) => CleanDeletedPlanEnvironmentsTask.RunTaskAsync(TimeSpan.FromHours(1), childLogger),
                TimeSpan.FromMinutes(10));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Job: Delete secret stores in deleted plans more than 7 days old
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.WatchDeletedPlanSecretStoresTask}_run",
                (childLogger) => WatchDeletedPlanSecretStoresTask.RunTaskAsync(TimeSpan.FromDays(1), childLogger),
                TimeSpan.FromMinutes(10));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Job: Watch Soft Deleted Environments to be Hard Deleted
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.WatchSoftDeletedEnvironmentToBeHardDeletedTask}_run",
                (childLogger) => WatchSoftDeletedEnvironmentToBeHardDeletedTask.RunTaskAsync(TimeSpan.FromMinutes(10), childLogger),
                TimeSpan.FromMinutes(10));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Job: Watch environments that needs an update
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.WatchEnvironmentsToBeUpdatedTask}_run",
                (childLogger) => WatchEnvironmentsToBeUpdatedTask.RunTaskAsync(TimeSpan.FromMinutes(5), childLogger),
                TimeSpan.FromHours(2));

            // new job continuation handlers
            JobQueueConsumerFactory.RegisterJobHandlers(JobHandlerTargets);
            JobQueueConsumerFactory.Start(JobHandlerTargets, QueueMessageProducerSettings.Default);

            // Job: Watch Export Blobs to be deleted from export storage account and wipe out export blob url from DB
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.WatchExportBlobsTask}_run",
                (childLogger) => WatchExportBlobsTask.RunTaskAsync(TimeSpan.FromDays(1), childLogger),
                TimeSpan.FromDays(1));
        }
    }
}
