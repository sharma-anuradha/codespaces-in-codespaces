// <copyright file="MockBillingSubmissionCloudStorageClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class MockBillingSubmissionCloudStorageClient : IBillingSubmissionCloudStorageClient
    {
        /// <inheritdoc/>
        public Task<bool> CheckForErrorsOnQueue()
        {
            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public Task<BillingSummaryTableSubmission> GetBillingTableSubmission(string partitionKey, string rowKey)
        {
            var dummySub = new BillingSummaryTableSubmission();
            return Task.FromResult(dummySub);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<BillSubmissionErrorResult>> GetSubmissionErrors()
        {
            return Task.FromResult(Enumerable.Empty<BillSubmissionErrorResult>());
        }

        /// <inheritdoc/>
        public Task<BillingSummaryTableSubmission> InsertOrUpdateBillingTableSubmission(BillingSummaryTableSubmission billingTableSubmission)
        {
            return Task.FromResult(billingTableSubmission);
        }

        /// <inheritdoc/>
        public Task PushBillingQueueSubmission(BillingSummaryQueueSubmission queueSubmission)
        {
            return Task.CompletedTask;
        }
    }
}
