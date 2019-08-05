using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Base class for all our hub services
    /// </summary>
    public class HubService<THub>
    {
        protected HubService(
            string serviceId,
            IEnumerable<IHubContextHost> hubContextHosts,
            ILogger logger)
        {
            Requires.NullOrNotNullElements(hubContextHosts, nameof(hubContextHosts));

            HubContextHosts = hubContextHosts.Where(hCtxt => hCtxt.HubType == typeof(THub)).ToArray();
            Logger = Requires.NotNull(logger, nameof(logger));
            ServiceId = serviceId;

            logger.LogInformation($"Service created with id:{ServiceId}");
        }

        public string ServiceId { get; }

        public ILogger Logger { get; }

        public IHubContextHost[] HubContextHosts { get; }

        /// <summary>
        /// Return all client proxies from a connection id
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public IEnumerable<IClientProxy> Clients(string connectionId)
        {
            return HubClients(hubClients => hubClients.Client(connectionId));
        }

        public IEnumerable<IClientProxy> HubClients(Func<IHubClients<IClientProxy>, IClientProxy> hubClientsCallback)
        {
            return HubContextHosts
                .Select(hCtxt => hubClientsCallback(hCtxt.Clients))
                .Where(clientProxy => clientProxy != null);
        }
    }
}
