// <copyright file="WarmupService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Manage the overall state of warmed up services
    /// </summary>
    public class WarmupService
    {
        private readonly IList<IAsyncWarmup> warmupServices;

        public WarmupService(IList<IAsyncWarmup> warmupServices)
        {
            this.warmupServices = warmupServices;
        }

        public Task CompletedAsync()
        {
            return WarmupUtility.WhenAllWarmupCompletedAsync(this.warmupServices);
        }

        public async Task<bool> CompletedValueAsync()
        {
            await CompletedAsync();
            return this.warmupServices.OfType<IHealthStatusProvider>().FirstOrDefault(ws => !ws.IsHealthy) == null;
        }
    }
}
