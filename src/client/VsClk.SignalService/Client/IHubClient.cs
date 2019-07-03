using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Event to be raised when a connection is attempted
    /// </summary>
    public class AttemptConnectionEventArgs : EventArgs
    {
        internal AttemptConnectionEventArgs(int retries, int backoffTimeMillisecs, Exception error)
        {
            Retries = retries;
            BackoffTimeMillisecs = backoffTimeMillisecs;
            Error = error;
        }

        public int Retries { get; }
        public int BackoffTimeMillisecs { get; set; }
        public Exception Error { get; }
    }

    /// <summary>
    /// The hub client interface
    /// </summary>
    public interface IHubClient : IAsyncDisposable
    {
        event AsyncEventHandler ConnectionStateChanged;
        event AsyncEventHandler<AttemptConnectionEventArgs> AttemptConnection;

        HubConnectionState State { get; }
        bool IsConnected { get; }
        bool IsRunning { get; }

        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);
    }
}
