// <copyright file="ResourceLoggingConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    /// Resource logging constants.
    /// </summary>
    public class ResourceLoggingConstants
    {
        /// <summary>
        /// Watch Orphaned Azure Resource Task.
        /// </summary>
        public const string DeleteResourceGroupDeploymentsTask = "delete_resource_group_deployments_task";

        /// <summary>
        /// Resource Broker.
        /// </summary>
        public const string ResourceBroker = "resource_broker";

        /// <summary>
        /// Resource Pool Manager.
        /// </summary>
        public const string ResourcePoolManager = "resource_pool_manager";

        /// <summary>
        /// Resource Heartbeat Manager.
        /// </summary>
        public const string ResourceHeartBeatManager = "resource_heartbeat_manager";

        /// <summary>
        /// Watch Continuation Task Worker Pool.
        /// </summary>
        public const string WatchContinuationTaskWorkerPool = "watch_continuation_task_worker_pool";

        /// <summary>
        /// Watch Pool Size Task.
        /// </summary>
        public const string WatchPoolSizeTask = "watch_pool_size_task";

        /// <summary>
        /// Watch pool size task.
        /// </summary>
        public const string WatchPoolVersionTask = "watch_pool_version_task";

        /// <summary>
        /// Watch pool state task.
        /// </summary>
        public const string WatchPoolStateTask = "watch_pool_state_task";

        /// <summary>
        /// Watch orphan pool task.
        /// </summary>
        public const string WatchOrphanedPoolTask = "watch_orphaned_pool_task";

        /// <summary>
        /// Watch pool state task.
        /// </summary>
        public const string WatchPoolSettingsTask = "watch_pool_settings_task";

        /// <summary>
        /// Watch Failed Resources Task.
        /// </summary>
        public const string WatchFailedResourcesTask = "watch_failed_resources_task";

        /// <summary>
        /// Watch Orphaned Azure Resource Task.
        /// </summary>
        public const string WatchOrphanedAzureResourceTask = "watch_orphaned_azure_resource_task";

        /// <summary>
        /// Watch Orphaned Build Artifact Image Task.
        /// </summary>
        public const string WatchOrphanedVmAgentImagesTask = "watch_orphaned_vm_agent_image_task";

        /// <summary>
        /// Watch Orphaned Build Artifact Storage Image Task.
        /// </summary>
        public const string WatchOrphanedStorageImagesTask = "watch_orphaned_storage_image_task";

        /// <summary>
        /// Watch Orphaned Build Artifact Compute Image Task.
        /// </summary>
        public const string WatchOrphanedComputeImagesTask = "watch_orphaned_compute_image_task";

        /// <summary>
        /// Watch Orphaned System Resource Task.
        /// </summary>
        public const string WatchOrphanedSystemResourceTask = "watch_orphaned_system_resource_task";

        /// <summary>
        /// Continuation Task Message Pump.
        /// </summary>
        public const string ContinuationTaskMessagePump = "continuation_task_message_pump";

        /// <summary>
        /// Continuation Task Worker Pool Manager name.
        /// </summary>
        public const string ContinuationTaskWorkerPoolManager = "continuation_task_worker_pool_manager";

        /// <summary>
        /// Continuation Task Worker name.
        /// </summary>
        public const string ContinuationTaskWorker = "continuation_task_worker";

        /// <summary>
        /// Continuation Task Activator name.
        /// </summary>
        public const string ContinuationTaskActivator = "continuation_task_activator";

        /// <summary>
        /// Start Continuation Task Message Handler name.
        /// </summary>
        public const string ContinuationTaskMessageHandlerStartEnvironment = "continuation_task_message_handler_start_envionment";

        /// <summary>
        /// Start Continuation Task Message Handler name.
        /// </summary>
        public const string ContinuationTaskMessageHandlerStartArchive = "continuation_task_message_handler_start_archive";

        /// <summary>
        /// Create Continuation Task Message Handler name.
        /// </summary>
        public const string ContinuationTaskMessageHandlerCreate = "continuation_task_message_handler_create";

        /// <summary>
        /// Delete Continuation Task Message Handler name.
        /// </summary>
        public const string ContinuationTaskMessageHandlerDelete = "continuation_task_message_handler_delete";

        /// <summary>
        /// Delete Orphaned Continuation Task Message Handler name.
        /// </summary>
        public const string ContinuationTaskMessageHandlerDeleteOrphaned = "continuation_task_message_handler_delete_orphaned";

        /// <summary>
        /// Cleanup Continuation Task Message Handler name.
        /// </summary>
        public const string ContinuationTaskMessageHandlerCleanup = "continuation_task_message_handler_cleanup";
    }
}
