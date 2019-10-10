// <copyright file="IBillingEventManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public interface IBillingEventManager
    {
        Task<BillingEvent> CreateEventAsync(
            VsoAccountInfo account,
            EnvironmentBillingInfo environment,
            string eventType,
            object args,
            IDiagnosticsLogger logger);

        Task<IEnumerable<VsoAccountInfo>> GetAccountsAsync(
            DateTime start,
            DateTime? end,
            IDiagnosticsLogger logger,
            ICollection<AzureLocation> locations);

        Task<IEnumerable<VsoAccountInfo>> GetAccountsByShardAsync(
            DateTime start,
            DateTime? end,
            IDiagnosticsLogger logger,
            ICollection<AzureLocation> locations,
            string shard);

        Task<IEnumerable<BillingEvent>> GetAccountEventsAsync(
            VsoAccountInfo account,
            DateTime start,
            DateTime? end,
            ICollection<string> eventTypes,
            IDiagnosticsLogger logger);

        Task<IEnumerable<BillingEvent>> GetAccountEventsAsync(
            Expression<Func<BillingEvent, bool>> filter,
            IDiagnosticsLogger logger);

        Task<BillingEvent> UpdateEventAsync(
            BillingEvent billingEvent,
            IDiagnosticsLogger logger);

        IEnumerable<string> GetShards();
    }
}
