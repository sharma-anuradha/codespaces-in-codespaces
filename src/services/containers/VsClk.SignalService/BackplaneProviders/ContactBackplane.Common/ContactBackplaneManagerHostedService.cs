using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Background long running service to wrap the IBackplaneManagerService instance
    /// </summary>
    public class ContactBackplaneManagerHostedService<T> : BackgroundService
    {
       public ContactBackplaneManagerHostedService(IContactBackplaneManager backplaneManager, T service)
        {
            // Note: we define the T as a service to be injected when this hosted service
            // is being constructed. The reason is that we want also the service to define
            // the 'MetricsFactory' property, otherwise the service could be created on demand 
            BackplaneManager = backplaneManager;
        }

        private IContactBackplaneManager BackplaneManager { get; }

        #region BackgroundService overrides

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return BackplaneManager.DisposeAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return BackplaneManager.RunAsync(stoppingToken);
        }

        #endregion
    }
}
