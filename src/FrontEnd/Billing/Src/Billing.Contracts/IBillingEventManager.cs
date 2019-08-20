// <copyright file="IBillingEventManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public interface IBillingEventManager
    {
        Task<BillingEvent> CreateEventAsync(
            VsoAccountInfo account,
            EnvironmentInfo environment,
            string eventType,
            object args,
            IDiagnosticsLogger logger);

        Task<IEnumerable<VsoAccountInfo>> GetAccountsAsync(
            DateTime start,
            DateTime? end,
            IDiagnosticsLogger logger);

        Task<IEnumerable<BillingEvent>> GetAccountEventsAsync(
            VsoAccountInfo account,
            DateTime start,
            DateTime? end,
            ICollection<string> eventTypes,
            IDiagnosticsLogger logger);
    }
}
