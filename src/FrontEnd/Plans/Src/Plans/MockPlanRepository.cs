// <copyright file="MockPlanRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    public class MockPlanRepository : MockRepository<VsoPlan>, IPlanRepository
    {
        public async Task<int> GetCountAsync(IDiagnosticsLogger logger)
        {
            return (await GetWhereAsync((model) => true, logger)).Count();
        }
    }
}
