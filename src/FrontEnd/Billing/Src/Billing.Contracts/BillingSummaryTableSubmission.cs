// <copyright file="BillingSummaryTableSubmission.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Cosmos.Table;
using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Represents an entry into Azure Table storage.
    /// </summary>
    public class BillingSummaryTableSubmission : TableEntity
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSummaryTableSubmission"/> class.
        /// </summary>
        public BillingSummaryTableSubmission()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSummaryTableSubmission"/> class.
        /// </summary>
        /// <param name="partionKey">used as the partitionKey</param>
        /// <param name="rowKey">used as the rowKey</param>
        public BillingSummaryTableSubmission(string partionKey, string rowKey)
        {
            PartitionKey = partionKey;
            RowKey = rowKey;
        }

        /// <summary>
        /// Gets or sets the Azure subscription ID.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets a randomly generated GUID representing a unique entry.
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Gets or sets the ending time for the billing entry.
        /// </summary>
        public DateTime EventDateTime { get; set; }

        /// <summary>
        /// Gets or sets the Meter ID we're using to charge.
        /// </summary>
        public string MeterID { get; set; }

        /// <summary>
        /// Gets or sets the ResourceURI.
        /// </summary>
        public string ResourceUri { get; set; }

        /// <summary>
        /// Gets or sets the Location where the resource is running.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the # of units to be billed.
        /// </summary>
        public double Quantity { get; set; }
    }
}
