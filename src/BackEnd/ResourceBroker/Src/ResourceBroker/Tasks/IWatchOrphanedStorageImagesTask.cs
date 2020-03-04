// <copyright file="IWatchOrphanedStorageImagesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// IWatchOrphanedArtifactStorageImagesTask to delete obsolete artifacts (Kitchensink images/blobs).
    /// </summary>
    public interface IWatchOrphanedStorageImagesTask : IBackgroundTask
    {
    }
}
