// <copyright file="ISyncRegionalEnvironmentsToGlobalTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Re-synchronoize regional cloud environment records back to the global repository where they are broken in the global repo.
    /// </summary>
    public interface ISyncRegionalEnvironmentsToGlobalTask : IBackgroundTask
    {
    }
}
