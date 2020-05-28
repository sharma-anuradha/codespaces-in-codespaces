// <copyright file="MockPlanRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// Mock plan repository.
    /// </summary>
    public class MockPlanRepository : MockRepository<VsoPlan>, IPlanRepository
    {
        /// <inheritdoc/>
        public async Task<int> GetCountAsync(IDiagnosticsLogger logger)
        {
            return (await GetWhereAsync((model) => true, logger)).Count();
        }

        /// <inheritdoc/>
        public Task<int> GetPlanSubscriptionCountAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<VsoPlan>> GetBillablePlansByShardAsync(string planShard, TimeSpan pagingDelay, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
