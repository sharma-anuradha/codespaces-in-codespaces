// <copyright file="BillSummaryManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Exposes methods that interact with the billing Summary repository.
    /// </summary>
    public class BillSummaryManager : IBillSummaryManager
    {
        private const string LogBaseName = "bill_summary_manager";

        private readonly IBillSummaryRepository billSummaryRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillSummaryManager"/> class.
        /// </summary>
        /// <param name="billSummaryRepository">bill summary repository.</param>
        public BillSummaryManager(IBillSummaryRepository billSummaryRepository)
        {
            Requires.NotNull(billSummaryRepository, nameof(billSummaryRepository));

            this.billSummaryRepository = billSummaryRepository;
        }

        /// <inheritdoc/>
        public Task<IEnumerable<BillSummary>> GetAllSummaries(string planId, DateTime endTime, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_all_bill_summaries",
                async (childLogger) =>
                {
                    return await billSummaryRepository.GetAllAsync(planId, endTime, logger);
                });
        }

        /// <inheritdoc />
        public Task<BillSummary> GetLatestBillSummaryAsync(string planId, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_latest_bill_summary",
                async (childLogger) =>
                {
                    return await billSummaryRepository.GetLatestAsync(planId, logger);
                });
        }

        /// <inheritdoc />
        public Task<BillSummary> CreateOrUpdateAsync(BillSummary billingSummary, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_create_or_update",
                async (childLogger) =>
                {
                    return await billSummaryRepository.CreateOrUpdateAsync(billingSummary, childLogger);
                });
        }
    }
}
