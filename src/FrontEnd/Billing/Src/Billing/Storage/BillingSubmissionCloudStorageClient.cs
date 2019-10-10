// <copyright file="BillingSubmissionCloudStorageClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// An interface for billing submission to underlying storage infrastructure
    /// </summary>
    public class BillingSubmissionCloudStorageClient : IBillingSubmissionCloudStorageClient
    {
        private readonly CloudTableClient cloudTableClient;
        private readonly IStorageQueueCollection cloudUsageQueue;
        private readonly IDiagnosticsLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSubmissionCloudStorageClient"/> class.
        /// </summary>
        /// <param name="cloudTableClient">the cloud table this collection operates on</param>
        /// <param name="cloudeUsageQueue">the usage queue</param>
        /// <param name="logger">the logger</param>
        public BillingSubmissionCloudStorageClient(CloudTableClient cloudTableClient, BillingSubmissionQueueCollection cloudeUsageQueue, IDiagnosticsLogger logger)
        {
            this.cloudTableClient = cloudTableClient;
            this.cloudUsageQueue = cloudeUsageQueue;
            this.logger = logger;
        }

        /// <inheritdoc />
        public Task<BillingSummaryTableSubmission> GetBillingTableSubmission(string partitionKey, string rowKey)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task PushBillingQueueSubmission(BillingSummaryQueueSubmission queueSubmission)
        {
            Requires.NotNull(queueSubmission, nameof(queueSubmission));
            await cloudUsageQueue.AddAsync(queueSubmission.ToJson(), null, logger);
        }

        /// <inheritdoc />
        public async Task<BillingSummaryTableSubmission> InsertOrUpdateBillingTableSubmission(BillingSummaryTableSubmission billingTableSubmission)
        {
            Requires.NotNull(billingTableSubmission, nameof(billingTableSubmission));

            try
            {
                // Create the InsertOrReplace table operation
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(billingTableSubmission);
                var cloudTable = cloudTableClient.GetTableReference("UsageReportingTable");

                // Execute the operation.
                TableResult result = await cloudTable.ExecuteAsync(insertOrMergeOperation);
                BillingSummaryTableSubmission insertedCustomer = result.Result as BillingSummaryTableSubmission;

                return insertedCustomer;
            }
            catch (StorageException e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }
    }
}
