using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class BillSubmissionErrorResult : TableEntity
    {

        /// <summary>
        /// This maps to our original PartitionKey
        /// </summary>
        public string UsageRecordPartitionKey { get; set; }

        /// <summary>
        /// This maps to our original RowKey
        /// </summary>
        public string UsageRecordRowKey { get; set; }

        /// <summary>
        /// This maps to our original BatchID
        /// </summary>
        public string BatchId { get; set; }

        /// <summary>
        /// This maps to the PA error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// This maps to the PA error code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// This maps to the Exception from PA
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// This maps to extra details from PA
        /// </summary>
        public string AdditionalInformation { get; set; }
    }
}
