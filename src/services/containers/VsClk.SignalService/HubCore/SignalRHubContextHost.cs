using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements IHubContextHost from the universal SignalRHub
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    /// <typeparam name="TSignalRHub"></typeparam>
    public class SignalRHubContextHost<THub, TSignalRHub> : IHubContextHost
        where THub : Hub
        where TSignalRHub : SignalRHub
    {
        private const string HubContextFieldName = "HubContextName";

        public SignalRHubContextHost(IHubContext<TSignalRHub> hubContext)
        {
            var hubName = HubType.GetField(HubContextFieldName).GetValue(null).ToString();
            Clients = new HubClientsProxy(hubContext.Clients, hubName);
            Groups = hubContext.Groups;
        }

        public Type HubType => typeof(THub);

        public IHubClients Clients { get; }
        public IGroupManager Groups { get; }

        /// <summary>
        /// Implements IHubClients that would be called from another hub into the SignalR hub
        /// </summary>
        private class HubClientsProxy : IHubClients
        {
            private readonly IHubClients hubClients;
            private readonly string hubName;

            public HubClientsProxy(IHubClients hubClients, string hubName)
            {
                this.hubClients = Requires.NotNull(hubClients, nameof(hubClients));
                Requires.NotNullOrEmpty(hubName, nameof(hubName));
                this.hubName = hubName;
            }

            #region IHubClients

            public IClientProxy All => ToClientProxy(this.hubClients.All);

            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
            {
                return ToClientProxy(this.hubClients.AllExcept(excludedConnectionIds));
            }

            public IClientProxy Client(string connectionId)
            {
                return ToClientProxy(this.hubClients.Client(connectionId));
            }

            public IClientProxy Clients(IReadOnlyList<string> connectionIds)
            {
                return ToClientProxy(this.hubClients.Clients(connectionIds));
            }

            public IClientProxy Group(string groupName)
            {
                return ToClientProxy(this.hubClients.Group(groupName));
            }

            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
            {
                return ToClientProxy(this.hubClients.GroupExcept(groupName, excludedConnectionIds));
            }

            public IClientProxy Groups(IReadOnlyList<string> groupNames)
            {
                return ToClientProxy(this.hubClients.Groups(groupNames));
            }

            public IClientProxy User(string userId)
            {
                return ToClientProxy(this.hubClients.User(userId));
            }

            public IClientProxy Users(IReadOnlyList<string> userIds)
            {
                return ToClientProxy(this.hubClients.Users(userIds));
            }

            #endregion

            private IClientProxy ToClientProxy(IClientProxy clientProxy)
            {
                return new ClientProxy(clientProxy, this.hubName);
            }
        }

        private class ClientProxy : IClientProxy
        {
            private readonly IClientProxy clientProxy;
            private readonly string hubName;

            public ClientProxy(IClientProxy clientProxy, string hubName)
            {
                this.clientProxy = Requires.NotNull(clientProxy, nameof(clientProxy));
                Requires.NotNullOrEmpty(hubName, nameof(hubName));
                this.hubName = hubName;
            }

            #region IClientProxy

            public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default)
            {
                return this.clientProxy.SendCoreAsync($"{this.hubName}.{method}", args, cancellationToken);
            }

            #endregion
        }
    }

}
