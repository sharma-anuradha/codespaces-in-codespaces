// <copyright file="IBillingArchivalManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Responsible for archiving billing records no longer actively needed.
    /// </summary>
    public interface IBillingArchivalManager
    {
        /// <summary>
        /// Migrates a single bill summary to archive storage.
        /// </summary>
        /// <param name="billSummary">Bill Summary.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Task.</returns>
        Task MigrateBillSummary(BillSummary billSummary, IDiagnosticsLogger logger);

        /// <summary>
        /// Migrates a single environment state change to archive storage.
        /// </summary>
        /// <param name="stateChange">Environment State Change.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Task.</returns>
        Task MigrateEnvironmentStateChange(EnvironmentStateChange stateChange, IDiagnosticsLogger logger);
    }
}