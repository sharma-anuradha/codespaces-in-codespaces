// <copyright file="IWatchSoftDeletedEnvironmentToBeHardDeletedTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Watch Soft Deleted Environments Task to be hard deleted. .
    /// </summary>
    public interface IWatchSoftDeletedEnvironmentToBeHardDeletedTask : IBackgroundTask
    {
    }
}
