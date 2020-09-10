// <copyright file="EnvironmentLoggingConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions
{
    /// <summary>
    /// Environment logging constants.
    /// </summary>
    public class EnvironmentLoggingConstants
    {
        /// <summary>
        /// Watch Orphaned System Environments Task.
        /// </summary>
        public const string WatchOrphanedSystemEnvironmentsTask = "watch_orphaned_system_environments_task";

        /// <summary>
        /// Watch Suspended Environments to be Archived Task.
        /// </summary>
        public const string WatchSuspendedEnvironmentsToBeArchivedTask = "watch_suspended_environments_to_be_archived_task";

        /// <summary>
        /// Watch Deleted Plan Environments Task.
        /// </summary>
        public const string WatchDeletedPlanEnvironmentsTask = "watch_deleted_plan_environments_task";

        /// <summary>
        /// Watch Failed Environment Task.
        /// </summary>
        public const string WatchFailedEnvironmentTask = "watch_failed_environment_task";

        /// <summary>
        /// Log Cloud Environments State Task.
        /// </summary>
        public const string LogCloudEnvironmentsStateTask = "log_cloud_environments_state_task";

        /// <summary>
        /// Log Subscription Statistics Task.
        /// </summary>
        public const string LogSubscriptionStatisticsTask = "log_subscription_statistics_task";

        /// <summary>
        /// Watch Soft Deleted Environments to be terminated Task.
        /// </summary>
        public const string WatchSoftDeletedEnvironmentToBeHardDeletedTask = "watch_soft_deleted_environments_to_be_hard_deleted_task";

        /// <summary>
        /// Watch secret stores to be deleted Task.
        /// </summary>
        public const string WatchDeletedPlanSecretStoresTask = "watch_deleted_plan_secret_stores_task";

        /// <summary>
        /// Watch export blobs Task.
        /// </summary>
        public const string WatchExportBlobsTask = "watch_export_blobs_task";

        /// <summary>
        /// Continuation Task Message Handler Archive.
        /// </summary>
        public const string ContinuationTaskMessageHandlerArchive = "continuation_task_message_handler_archive";

        /// <summary>
        /// Continuation Task Message Handler Start Environment.
        /// </summary>
        public const string ContinuationTaskMessageHandlerStartEnv = "continuation_task_message_handler_start_environment";

        /// <summary>
        /// Continuation Task Message Handler Start Environment V2.
        /// </summary>
        public const string ContinuationTaskMessageHandlerStartEnvV2 = "continuation_task_message_handler_start_environment_v2";

        /// <summary>
        /// Log Continuation Task Worker Pool Manager name.
        /// </summary>
        public const string ContinuationTaskWorkerPoolManager = "continuation_task_worker_pool_manager";

        /// <summary>
        /// Continuation Task Message Pump.
        /// </summary>
        public const string ContinuationTaskMessagePump = "continuation_task_message_pump";

        /// <summary>
        /// Refresh key vault secret cache task.
        /// </summary>
        public const string RefreshKeyVaultSecretCacheTask = "refresh_key_vault_secret_cache_task";

        /// <summary>
        /// Migrate cloud environments to their regional location task.
        /// </summary>
        public const string CloudEnvironmentRegionalMigrationTask = "cloud_environment_regional_migration_task";

        /// <summary>
        /// Re-syncbroken Global environment records with their regional environment records.
        /// </summary>
        public const string SyncRegionalEnvironmentsToGlobalTask = "cloud_environment_resync_regional_to_global_task";
    }
}
