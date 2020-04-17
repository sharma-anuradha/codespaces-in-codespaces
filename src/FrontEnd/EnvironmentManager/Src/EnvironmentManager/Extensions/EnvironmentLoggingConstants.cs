﻿// <copyright file="EnvironmentLoggingConstants.cs" company="Microsoft">
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
        /// Continuation Task Message Handler Archive.
        /// </summary>
        public const string ContinuationTaskMessageHandlerArchive = "continuation_task_message_handler_archive";

        /// <summary>
        /// Log Continuation Task Worker Pool Manager name.
        /// </summary>
        public const string ContinuationTaskWorkerPoolManager = "continuation_task_worker_pool_manager";

        /// <summary>
        /// Continuation Task Message Pump.
        /// </summary>
        public const string ContinuationTaskMessagePump = "continuation_task_message_pump";
    }
}
