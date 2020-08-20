// <copyright file="IWatchDeletedPlanSecretStoresTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Watch Secret Stores Task to be deleted.
    /// </summary>
    public interface IWatchDeletedPlanSecretStoresTask : IBackgroundTask
    {
    }
}