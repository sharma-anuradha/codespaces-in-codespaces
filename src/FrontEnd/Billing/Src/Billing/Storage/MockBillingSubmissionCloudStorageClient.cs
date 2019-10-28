// <copyright file="MockBillingSubmissionCloudStorageClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    class MockBillingSubmissionCloudStorageClient : IBillingSubmissionCloudStorageClient
    {
        public Task<bool> CheckForErrorsOnQueue()
        {
            return Task.FromResult(false);
        }

        public Task<BillingSummaryTableSubmission> GetBillingTableSubmission(string partitionKey, string rowKey)
        {
            var dummySub = new BillingSummaryTableSubmission();
            return Task.FromResult(dummySub);
        }

        public Task<IEnumerable<BillSubmissionErrorResult>> GetSubmissionErrors()
        {
            return Task.FromResult(Enumerable.Empty<BillSubmissionErrorResult>());

        }

        public Task<BillingSummaryTableSubmission> InsertOrUpdateBillingTableSubmission(BillingSummaryTableSubmission billingTableSubmission)
        {
            return Task.FromResult(billingTableSubmission);
        }

        public Task PushBillingQueueSubmission(BillingSummaryQueueSubmission queueSubmission)
        {
            return Task.CompletedTask;
        }
    }
}
