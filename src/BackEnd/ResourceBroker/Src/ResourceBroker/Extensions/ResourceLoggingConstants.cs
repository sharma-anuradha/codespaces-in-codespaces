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
        /// Base Continuation Task Message Handler name.
        /// </summary>
        public const string BaseContinuationTaskMessageHandler = "base_continuation_task_message_handler";

        /// <summary>
        /// Start Continuation Task Message Handler name.
        /// </summary>
        public const string ContinuationTaskMessageHandlerStart = "continuation_task_message_handler_start";

        /// <summary>
        /// Create Continuation Task Message Handler name.
        /// </summary>
        public const string ContinuationTaskMessageHandlerCreate = "continuation_task_message_handler_create";

        /// <summary>
        /// Delete Continuation Task Message Handler name.
        /// </summary>
        public const string ContinuationTaskMessageHandlerDelete = "continuation_task_message_handler_delete";
    }
}
