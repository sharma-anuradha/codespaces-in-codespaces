// <copyright file="IConnectionEstablishedMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Connections
{
    /// <summary>
    /// Session message handler to handle <see cref="Message"/>.
    /// </summary>
    public interface IConnectionEstablishedMessageHandler
    {
        /// <summary>
        /// Processes the connection established messages.
        /// </summary>
        /// <param name="message">The connection established message.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ProcessSessionMessageAsync(Message message, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}