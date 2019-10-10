// <copyright file="IBillingSummarySubmissionService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{

    public interface IBillingSummarySubmissionService
    {
        /// <summary>
        /// Processes a sharded set of billing summaries for the current control plane.
        /// </summary>
        /// <returns>Task to track completion</returns>
        Task ProcessBillingSummaries();

    }
}
