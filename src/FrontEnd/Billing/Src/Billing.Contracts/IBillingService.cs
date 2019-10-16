﻿// <copyright file="IBillingService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Interface defining the BillingWorker contract.
    /// </summary>
    public interface IBillingService
    {
        /// <summary>
        /// Generate billing summary events.
        /// </summary>
        /// <param name="cancellationToken">Notification that the Task should stop.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        Task GenerateBillingSummaryAsync(CancellationToken cancellationToken);
    }
}
