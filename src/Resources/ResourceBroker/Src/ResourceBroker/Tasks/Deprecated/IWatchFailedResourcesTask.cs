// <copyright file="IWatchFailedResourcesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for failed resources.
    /// </summary>
    public interface IWatchFailedResourcesTask : IBackgroundTask
    {
    }
}
