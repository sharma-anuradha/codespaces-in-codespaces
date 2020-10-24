// <copyright file="ICloudEnvironmentRegionalMigrationTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Migrate cloud environments to their regional repositories.
    /// </summary>
    public interface ICloudEnvironmentRegionalMigrationTask : IBackgroundTask
    {
    }
}
