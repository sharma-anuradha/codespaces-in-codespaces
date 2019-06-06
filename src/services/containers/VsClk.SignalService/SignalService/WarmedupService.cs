using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Base class for all our background services
    /// </summary>
    public abstract class WarmedUpService : BackgroundService, IAsyncWarmup, IHealthStatusProvider
    {
        private readonly TaskCompletionSource<bool> warmedUpResult = new TaskCompletionSource<bool>();

        public virtual bool State => this.warmedUpResult.Task.IsCompleted && this.warmedUpResult.Task.Result;

        protected WarmedUpService(
            IList<IAsyncWarmup> warmupServices, 
            IList<IHealthStatusProvider> healthStatusProviders)
        {
            warmupServices.Add(this);
            healthStatusProviders.Add(this);
        }

        protected void CompleteWarmup(bool result)
        {
            this.warmedUpResult.TrySetResult(result);
        }

        #region IAsyncWarmup

        public Task WarmupCompletedAsync()
        {
            return this.warmedUpResult.Task;
        }

        #endregion
    }
}
