// <copyright file="NewConnectionsSessionClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.ServiceBus;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Clients
{
    /// <summary>
    /// Provides access to lazy loaded singleton <see cref="ISessionClient" /> for connections-new queue.
    /// </summary>
    public class NewConnectionsSessionClientProvider : ServiceBusSessionClientProviderBase, INewConnectionsSessionClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewConnectionsSessionClientProvider"/> class.
        /// </summary>
        /// <param name="serviceBusClientProvider">The service bus client provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public NewConnectionsSessionClientProvider(
            IServiceBusClientProvider serviceBusClientProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(QueueNames.NewConnections, serviceBusClientProvider, loggerFactory, defaultLogValues)
        {
        }
    }
}
