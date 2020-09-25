// <copyright file="JobQueueIds.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.Constants
{
    /// <summary>
    /// Set of common QueueIds to support communication across components.
    /// </summary>
    /// <remarks>
    /// Cross-component calls should be avoided as much as possible, queues which are only used by one component should not use these ids.
    /// </remarks>
    public static class JobQueueIds
    {
        public const string ConfirmOrphanedSystemResourceJob = "jobhandler-confirm-orphaned-system-resource";
    }
}
