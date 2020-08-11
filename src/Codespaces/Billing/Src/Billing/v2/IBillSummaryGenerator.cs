// <copyright file="IBillSummaryGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Interface for the BillingSummaryGenerator.
    /// </summary>
    public interface IBillSummaryGenerator
    {
        /// <summary>
        /// Generates a billing summary for a given billing Summary request.
        /// </summary>
        /// <param name="billingSummaryRequest">the billing summary request that a bill should be generated for.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>a task indicating success.</returns>
        Task GenerateBillingSummaryAsync(BillingSummaryRequest billingSummaryRequest, IDiagnosticsLogger logger);
    }
}
