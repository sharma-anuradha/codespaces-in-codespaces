using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Our client proxy base class
    /// </summary>
    public class HubProxyBase
    {
        private readonly string hubName;
        private const string InvokeHubMethodAsync = "InvokeHubMethodAsync";

        protected HubProxyBase(HubConnection connection, string hubName)
        {
            Connection = Requires.NotNull(connection, nameof(connection));
            this.hubName = hubName;
        }

        protected HubConnection Connection { get; }

        protected string ToHubMethodName(string methodName)
        {
            return string.IsNullOrEmpty(this.hubName) ? methodName : $"{this.hubName}.{methodName}";
        }

        protected async Task<T> InvokeAsync<T>(string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(this.hubName))
            {
                return (T)(await Connection.InvokeCoreAsync(methodName, typeof(T), args, cancellationToken).ConfigureAwait(false));
            }
            else
            {
                return (T)(await Connection.InvokeCoreAsync(InvokeHubMethodAsync, typeof(T), new object[] { ToHubMethodName(methodName), args }, cancellationToken));
            }
        }

        protected Task InvokeAsync(string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(this.hubName))
            {
                return Connection.InvokeCoreAsync(methodName, typeof(object), args, cancellationToken);
            }
            else
            {
                return Connection.InvokeCoreAsync(InvokeHubMethodAsync, typeof(object), new object[] { ToHubMethodName(methodName), args }, cancellationToken);
            }
        }
    }
}
