// <copyright file="IWatchOrphanedSystemEnvironmentsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Watch Orphaned System Environments Task.
    /// </summary>
    public interface IWatchOrphanedSystemEnvironmentsTask : IBackgroundTask
    {
    }
}
