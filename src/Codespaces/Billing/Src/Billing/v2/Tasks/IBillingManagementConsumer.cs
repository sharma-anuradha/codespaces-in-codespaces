// <copyright file="IBillingManagementConsumer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Handles Billing Management queue requests.
    /// </summary>
    public interface IBillingManagementConsumer : IJobHandler
    {
    }
}
