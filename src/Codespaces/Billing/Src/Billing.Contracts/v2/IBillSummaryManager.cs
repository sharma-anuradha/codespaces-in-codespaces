// <copyright file="IBillSummaryManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// interface for interacting with the billing Summary manager.
    /// </summary>
    public interface IBillSummaryManager
    {
        /// <summary>
        /// Gets the latest billing summary for a given planId.
        /// </summary>
        /// <param name="planId">The plan we want the latest billing summary for.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>the latest billing summary.</returns>
        Task<BillSummary> GetLatestBillSummaryAsync(string planId, IDiagnosticsLogger logger);

        /// <summary>
        /// Creates or updates an existing bill summary.
        /// </summary>
        /// <param name="billingSummary">the billing summary being created or updated.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>a task indicating completion.</returns>
        Task<BillSummary> CreateOrUpdateAsync(BillSummary billingSummary, IDiagnosticsLogger logger);

        /// <summary>
        /// Retrieves all bill summaries through a given time window.
        /// </summary>
        /// <param name="planId">planId.</param>
        /// <param name="endTime">endTime.</param>
        /// <param name="logger">logger.</param>
        /// <returns>Task.</returns>
        Task<IEnumerable<BillSummary>> GetAllSummaries(string planId, DateTime endTime, IDiagnosticsLogger logger);
    }
}
