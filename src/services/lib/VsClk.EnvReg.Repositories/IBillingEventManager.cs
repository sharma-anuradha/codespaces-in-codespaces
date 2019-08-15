using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsCloudKernel.Services.VsClk.EnvReg.Repositories
{
    public interface IBillingEventManager
    {
        Task<BillingEvent> CreateEventAsync(
            BillingAccountInfo account,
            EnvironmentInfo environment,
            string eventType,
            object args,
            IDiagnosticsLogger logger);

        Task<IEnumerable<BillingAccountInfo>> GetAccountsAsync(
            DateTime start,
            DateTime? end,
            IDiagnosticsLogger logger);

        Task<IEnumerable<BillingEvent>> GetAccountEventsAsync(
            BillingAccountInfo account,
            DateTime start,
            DateTime? end,
            ICollection<string> eventTypes,
            IDiagnosticsLogger logger);
    }
}
