// <copyright file="IBillSummaryRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository
{
    /// <summary>
    /// the interface for the billing summary repository.
    /// </summary>
    public interface IBillSummaryRepository : IDocumentDbCollection<BillSummary>
    {
        /// <summary>
        /// Gets the latest billing summary for a given planId.
        /// </summary>
        /// <param name="planId"> the planId we are looking for the latest billing summary of.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>the latest billing summary.</returns>
        Task<BillSummary> GetLatestAsync(string planId, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the latest billing summary for a given planId.
        /// </summary>
        /// <param name="planId"> the planId we are looking for the latest billing summary of.</param>
        /// <param name="endTime">the end time we want events events preceeding.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>the latest billing summary.</returns>
        Task<IEnumerable<BillSummary>> GetAllAsync(string planId, DateTime endTime, IDiagnosticsLogger logger);
    }
}
