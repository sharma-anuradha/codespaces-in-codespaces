// <copyright file="ResourceLoggingConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    public class ResourceLoggingsConstants
    {
        public const string ContinuationTaskActivator = "continuation-task-activator";

        /// <summary>
        ///
        /// </summary>
        public const string ContinuationTaskMessagePump = "continuation-task-message-pump";

        /// <summary>
        ///
        /// </summary>
        public const string ContinuationTaskWorkerPoolManager = "continuation-task-worker-pool-manager";

        /// <summary>
        /// Continuation Task Worker name;
        /// </summary>
        public const string ContinuationTaskWorker = "continuation-task-worker";
    }

    /// <summary>
    /// Logging constants for resource broker.
    /// </summary>
    public class ResourceLoggingPropertyConstants
    {
        /// <summary>
        /// Resource location name.
        /// </summary>
        public const string ResourceLocation = nameof(ResourceLocation);

        /// <summary>
        /// Resource sku name.
        /// </summary>
        public const string ResourceSkuName = nameof(ResourceSkuName);

        /// <summary>
        /// Resource type.
        /// </summary>
        public const string ResourceType = nameof(ResourceType);
    }
}
