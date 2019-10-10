// <copyright file="MockBillingSubmissionCloudStorageClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    class MockBillingSubmissionCloudStorageClient : IBillingSubmissionCloudStorageClient
    {
        public Task<BillingSummaryTableSubmission> GetBillingTableSubmission(string partitionKey, string rowKey)
        {
            throw new System.NotImplementedException();
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
