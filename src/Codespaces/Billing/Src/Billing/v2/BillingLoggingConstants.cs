// <copyright file="BillingLoggingConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Management.Network.Fluent.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Billing Logging Constants.
    /// </summary>
    public class BillingLoggingConstants
    {
        /// <summary>
        /// Billing Management Queue.
        /// </summary>
        public const string BillingManagermentQueue = "billing-v2-management-queue";

        /// <summary>
        /// Billing Plan Batch Queue.
        /// </summary>
        public const string BillingPlanBatchQueue = "billing-v2-plan-batch-queue";

        /// <summary>
        /// Billing Plan Summary Queue.
        /// </summary>
        public const string BillingPlanSummaryQueue = "billing-v2-plan-summary-queue";

        /// <summary>
        /// Billing Plan Cleanup Queue.
        /// </summary>
        public const string BillingPlanCleanupQueue = "billing-v2-plan-cleanup-queue";

        /// <summary>
        /// Billing Task.
        /// </summary>
        public const string BillingManagementTask = "billing_management_task";

        /// <summary>
        /// Billing Plan Batch Task.
        /// </summary>
        public const string BillingPlanBatchTask = "billing_plan_batch_task";

        /// <summary>
        /// Billing Plan Summary Task.
        /// </summary>
        public const string BillingPlanSummaryTask = "billing_plan_summary_task";

        /// <summary>
        /// Billing Plan Cleanup Task.
        /// </summary>
        public const string BillingPlanCleanupTask = "billing_plan_cleanup_task";

        /// <summary>
        /// Plan ID (Guid) field, not the resource ID.
        /// </summary>
        public const string PlanId = "PlanId";

        /// <summary>
        /// The data plane location.
        /// </summary>
        public const string Location = "Location";

        /// <summary>
        /// The shard value.
        /// </summary>
        public const string Shard = "Shard";

        /// <summary>
        /// The desired end time.
        /// </summary>
        public const string BillEndingTime = "BillEndingTime";
    }
}
