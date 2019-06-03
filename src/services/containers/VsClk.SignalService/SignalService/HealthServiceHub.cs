using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Echo result type with additional info on the hub that perform the echo
    /// </summary>
    public class EchoResult
    {
        public string Stamp { get; set; }
        public string ServiceId { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// A hub to expose some methods to verify the health of the overall signalr service
    /// </summary>
    public class HealthServiceHub : Hub
    {
        private readonly AppSettings appSettings;
        private readonly PresenceService presenceService;

        public HealthServiceHub(
            IOptions<AppSettings> appSettingsProvider,
            PresenceService presenceService)
        {
            this.appSettings = appSettingsProvider.Value;
            this.presenceService = presenceService;
        }

        public EchoResult Echo(string message)
        {
            return new EchoResult()
            {
                Stamp = this.appSettings.Stamp,
                ServiceId = this.presenceService.ServiceId,
                Message = message
            };
        }
    }
}
