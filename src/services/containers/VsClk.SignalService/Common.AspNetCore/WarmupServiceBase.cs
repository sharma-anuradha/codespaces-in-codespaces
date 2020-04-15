// <copyright file="WarmupServiceBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Base class for all our background services
    /// </summary>
    public abstract class WarmupServiceBase : BackgroundService, IAsyncWarmup, IHealthStatusProvider
    {
        private readonly TaskCompletionSource<bool> warmedUpResult = new TaskCompletionSource<bool>();

        protected WarmupServiceBase(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders)
        {
            warmupServices.Add(this);
            healthStatusProviders.Add(this);
        }

        public virtual bool IsHealthy => this.warmedUpResult.Task.IsCompleted && HealthState;

        public virtual object Status => IsHealthy;

        protected bool HealthState { get; set; }

        public Task WarmupCompletedAsync()
        {
            return this.warmedUpResult.Task;
        }

        protected void CompleteWarmup(bool result)
        {
            HealthState = result;
            this.warmedUpResult.TrySetResult(result);
        }
    }
}
