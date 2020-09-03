// <copyright file="IBillSummaryScrubber.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Scrubs bill summarys and environment state changes table.
    /// </summary>
    public interface IBillSummaryScrubber
    {
        /// <summary>
        /// Srubs a specific plans records.
        /// </summary>
        /// <param name="request">identifies which plan needs to have it's records scrubbed.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>A task indicating completion.</returns>
        public Task ScrubBillSummariesForPlanAsync(BillScrubberRequest request, IDiagnosticsLogger logger);
    }
}