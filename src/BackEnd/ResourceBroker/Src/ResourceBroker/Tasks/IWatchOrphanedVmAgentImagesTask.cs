// <copyright file="IWatchOrphanedVmAgentImagesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// IWatchOrphanedArtifactImagesTask to delete obsolete artifacts (VSO agent images/blobs).
    /// </summary>
    public interface IWatchOrphanedVmAgentImagesTask : IBackgroundTask
    {
    }
}
