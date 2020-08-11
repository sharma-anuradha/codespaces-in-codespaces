// <copyright file="IBillingManagementProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Trigger billing management task.
    /// </summary>
    public interface IBillingManagementProducer
    {
        /// <summary>
        /// Publish job of Billing Management queue.
        /// </summary>
        /// <param name="logger">Target Logger.</param>
        /// <param name="cancellationToken">Target Cancellation Token.</param>
        /// <returns>Running task.</returns>
        Task PublishJobAsync(
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken);
    }
}
