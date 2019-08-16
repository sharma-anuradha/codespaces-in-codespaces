using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace VsClk.EnvReg.Repositories
{
    public interface IAccountManager
    {
        Task<BillingAccount> CreateOrUpdateAsync(BillingAccount model, IDiagnosticsLogger logger);

        Task<IEnumerable<BillingAccount>> GetAsync(BillingAccountInfo account,IDiagnosticsLogger logger);

        Task<bool> DeleteAsync(BillingAccountInfo account, IDiagnosticsLogger logger);

        Task<IEnumerable<BillingAccount>> GetListAsync(string subscriptionId, string resourceGroup, IDiagnosticsLogger logger);

        Task<IEnumerable<BillingAccount>> GetListBySubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger);
    }
}
