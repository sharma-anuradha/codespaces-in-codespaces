﻿// <copyright file="BillingSubmissionCloudStorageClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Storage;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// An interface for billing submission to underlying storage infrastructure
    /// </summary>
    public class BillingSubmissionCloudStorageClient : IBillingSubmissionCloudStorageClient
    {
        private const string ErrorReportingTableName = "ErrorReportingTable";
        private const string UsageReportingTableName = "UsageReportingTable";
        private readonly CloudTableClient cloudTableClient;
        private readonly IStorageQueueCollection cloudUsageQueue;
        private readonly BillingSubmissionErrorQueueCollection cloudErrorQueue;
        private readonly IDiagnosticsLogger logger;
        private readonly JsonSerializer jsonSerializer = new JsonSerializer();

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSubmissionCloudStorageClient"/> class.
        /// </summary>
        /// <param name="cloudTableClient">the cloud table this collection operates on</param>
        /// <param name="cloudeUsageQueue">the usage queue</param>
        /// <param name="cloudErrorQueue"> the error queue</param>
        /// <param name="logger">the logger</param>
        public BillingSubmissionCloudStorageClient(CloudTableClient cloudTableClient, BillingSubmissionQueueCollection cloudeUsageQueue, BillingSubmissionErrorQueueCollection cloudErrorQueue, IDiagnosticsLogger logger)
        {
            this.cloudTableClient = cloudTableClient;
            this.cloudUsageQueue = cloudeUsageQueue;
            this.cloudErrorQueue = cloudErrorQueue;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<BillingSummaryTableSubmission> GetBillingTableSubmission(string partitionKey, string rowKey)
        {
            try
            {
                var cloudTable = cloudTableClient.GetTableReference(UsageReportingTableName);

                TableOperation retrieve = TableOperation.Retrieve<BillingSummaryTableSubmission>(partitionKey, rowKey);

                TableResult result = await cloudTable.ExecuteAsync(retrieve);
                return result.Result as BillingSummaryTableSubmission;
            }
            catch (Exception e)
            {
                logger.LogErrorWithDetail("BillSubmission-Table-Retrieve-Error", e.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task PushBillingQueueSubmission(BillingSummaryQueueSubmission queueSubmission)
        {
            Requires.NotNull(queueSubmission, nameof(queueSubmission));
            await cloudUsageQueue.AddAsync(queueSubmission.ToJson(), null, logger);
        }

        /// <inheritdoc />
        public async Task<bool> CheckForErrorsOnQueue()
        {
            var messageCount = await cloudErrorQueue.GetApproximateMessageCount(logger);
            return messageCount > 0;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<BillSubmissionErrorResult>> GetSubmissionErrors()
        {
            var message = await cloudErrorQueue.GetAsync(logger);
            var result = JsonConvert.DeserializeObject(message.AsString, typeof(BillSubmissionErrorQueueResult)) as BillSubmissionErrorQueueResult;

            var errorMessages = GetBillingErrors(result.PartitionId);

            // Delete the message out of the error queue.
            await cloudErrorQueue.DeleteAsync(message, logger);

            return errorMessages;
        }

        private IEnumerable<BillSubmissionErrorResult> GetBillingErrors(string partitionKey)
        {
            // Get query resady
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            TableQuery<BillSubmissionErrorResult> query = new TableQuery<BillSubmissionErrorResult>().Where(filter);

            // Query against the whole error table.
            var cloudTable = cloudTableClient.GetTableReference(ErrorReportingTableName);
            return cloudTable.ExecuteQuery<BillSubmissionErrorResult>(query);
        }

        /// <inheritdoc />
        public async Task<BillingSummaryTableSubmission> InsertOrUpdateBillingTableSubmission(BillingSummaryTableSubmission billingTableSubmission)
        {
            Requires.NotNull(billingTableSubmission, nameof(billingTableSubmission));

            try
            {
                // Create the InsertOrReplace table operation
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(billingTableSubmission);
                var cloudTable = cloudTableClient.GetTableReference(UsageReportingTableName);

                // Execute the operation.
                TableResult result = await cloudTable.ExecuteAsync(insertOrMergeOperation);
                BillingSummaryTableSubmission insertedCustomer = result.Result as BillingSummaryTableSubmission;

                return insertedCustomer;
            }
            catch (StorageException e)
            {
                logger.LogErrorWithDetail("BillSubmission-Table-Insert-Error", e.Message);
                throw;
            }
        }
    }
}
