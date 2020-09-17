// <copyright file="IDeleteResourceGroupDeploymentsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Deletes deployment history on Resource Groups in the data plane subscriptions
    /// to avoid hitting the 800 deployment history limit on Azure Resource Groups.
    /// </summary>
    public interface IDeleteResourceGroupDeploymentsTask : IBackgroundTask
    {
    }
}
