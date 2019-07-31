using System;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Define an abstraction that would have access to a hub context
    /// </summary>
    public interface IHubContextHost
    {
        /// <summary>
        /// Target hub type definition
        /// </summary>
        Type HubType { get; }

        /// <summary>
        /// Return the hub underlying clients
        /// </summary>
        IHubClients Clients { get; }

        /// <summary>
        /// Return the hub underlying groups
        /// </summary>
        IGroupManager Groups { get; }
    }

    /// <summary>
    /// Simple IHubContextHost implementation from a standard hub being defined
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public class HubContextHost<THub> : IHubContextHost where THub : Hub
    {
        public HubContextHost(IHubContext<THub> hubContext)
        {
            Clients = hubContext.Clients;
            Groups = hubContext.Groups;
        }

        #region IHubContextHost

        public Type HubType => typeof(THub);
        public IHubClients Clients { get; }
        public IGroupManager Groups { get; }

        #endregion
    }
}
