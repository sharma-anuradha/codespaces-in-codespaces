// <copyright file="IBillingSummarySubmissionService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Interface for billing submission functionality
    /// </summary>
    public interface IBillingSummarySubmissionService
    {
        /// <summary>
        /// Processes a sharded set of billing summaries for the current control plane.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task to track completion</returns>
        Task ProcessBillingSummariesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Checks for Billing submission errors
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task to track completion</returns>
        Task CheckForBillingSubmissionErorrs(CancellationToken cancellationToken);
    }
}
