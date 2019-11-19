using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Background hosted service to control the backplane sertvice lifetime
    /// </summary>
    public class ContactBackplaneHostedService : BackgroundService
    {
        public ContactBackplaneHostedService(ContactBackplaneService contactBackplaneService)
        {
            ContactBackplaneService = Requires.NotNull(contactBackplaneService, nameof(contactBackplaneService));
        }

        private ContactBackplaneService ContactBackplaneService { get; }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return ContactBackplaneService.DisposeAsync();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return ContactBackplaneService.RunAsync(stoppingToken);
        }
    }
}
