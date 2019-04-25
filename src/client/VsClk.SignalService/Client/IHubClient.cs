using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// The hub client interface
    /// </summary>
    public interface IHubClient : IAsyncDisposable
    {
        event AsyncEventHandler ConnectionStateChanged;

        HubConnectionState State { get; }
        bool IsConnected { get; }
        bool IsRunning { get; }

        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);
    }
}
