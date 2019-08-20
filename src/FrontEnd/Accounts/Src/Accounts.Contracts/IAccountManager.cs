// <copyright file="IAccountManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    public interface IAccountManager
    {
        Task<VsoAccount> CreateOrUpdateAsync(VsoAccount model, IDiagnosticsLogger logger);

        Task<IEnumerable<VsoAccount>> GetAsync(VsoAccountInfo account, IDiagnosticsLogger logger);

        Task<bool> DeleteAsync(VsoAccountInfo account, IDiagnosticsLogger logger);

        Task<IEnumerable<VsoAccount>> GetListAsync(string subscriptionId, string resourceGroup, IDiagnosticsLogger logger);

        Task<IEnumerable<VsoAccount>> GetListBySubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger);
    }
}
