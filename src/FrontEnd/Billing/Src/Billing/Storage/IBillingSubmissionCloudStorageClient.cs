// <copyright file="IBillingSubmissionCloudStorageClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// An interface that represents the cloud storage client
    /// </summary>
    public interface IBillingSubmissionCloudStorageClient
    {
        /// <summary>
        /// Gets a particular billing table submission based on the partitionKey and rowkey.
        /// </summary>
        /// <param name="partitionKey">This is the subscriptionID</param>
        /// <param name="rowKey">This is the billingSummaryID</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<BillingSummaryTableSubmission> GetBillingTableSubmission(string partitionKey, string rowKey);

        /// <summary>
        /// Inserts a billing table submission into the appropriate SA's table
        /// </summary>
        /// <param name="billingTableSubmission">the billing table submission to submit</param>
        /// <returns>the billing summary if it was added to the the billing submission table</returns>
        Task<BillingSummaryTableSubmission> InsertOrUpdateBillingTableSubmission(BillingSummaryTableSubmission billingTableSubmission);

        /// <summary>
        /// Pushes a billing queue submission to the appropriate queue
        /// </summary>
        /// <param name="queueSubmission">the queue entry to submit</param>
        /// <returns>a task that completed when the push completes</returns>
        Task PushBillingQueueSubmission(BillingSummaryQueueSubmission queueSubmission);

        /// <summary>
        /// Checks if there's table submission errors on the queue.
        /// </summary>
        /// <returns> true if there's any errors in the queue</returns>
        Task<bool> CheckForErrorsOnQueue();

        /// <summary>
        /// Gets the next set of errors for the next 1 entry on the error PA queue.
        /// </summary>
        /// <returns>all the errors for a given queue.</returns>
        Task<IEnumerable<BillSubmissionErrorResult>> GetSubmissionErrors();
    }
}
