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
        public const string ContinuationTaskMessagePump = "continuation-task-message-pump";

        /// <summary>
        /// Continuation Task Worker Pool Manager name.
        /// </summary>
        public const string ContinuationTaskWorkerPoolManager = "continuation-task-worker-pool-manager";

        /// <summary>
        /// Continuation Task Worker name.
        /// </summary>
        public const string ContinuationTaskWorker = "continuation-task-worker";

        /// <summary>
        /// Continuation Task Activator name.
        /// </summary>
        public const string ContinuationTaskActivator = "continuation-task-activator";

        /// <summary>
        /// Base Continuation Task Message Handler name.
        /// </summary>
        public const string BaseContinuationTaskMessageHandler = "base-continuation-task-message-handler";
    }
}
