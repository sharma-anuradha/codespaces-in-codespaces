// <copyright file="PortForwardingConnectionEstablished.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// The show subscriptions verb.
    /// </summary>
    [Verb("pf-connection-established", HelpText = "Post connection established message to the queue.")]
    public class PortForwardingConnectionEstablished : CommandBase
    {
        /// <summary>
        /// Gets or sets connection workspace id.
        /// </summary>
        [Option('w', "workspace", HelpText = "Workspace Id")]
        public string WorkspaceId { get; set; }

        /// <summary>
        /// Gets or sets connection destination:source port.
        /// </summary>
        [Option('p', "port", HelpText = "<destination_port>:<source_port>")]
        public string Port { get; set; }

        /// <summary>
        /// Gets or sets agent name.
        /// </summary>
        [Option('n', "agent-name", HelpText = "Agent name")]
        public string AgentName { get; set; }

        /// <summary>
        /// Gets or sets agent uid.
        /// </summary>
        [Option('u', "agent-uid", HelpText = "Agent Uid")]
        public string AgentUid { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteCommandAsync(services, stdout, stderr).Wait();
        }

        private async Task ExecuteCommandAsync(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var queueClientProvider = services.GetService<IServiceBusQueueClientProvider>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logger = loggerFactory.New();
            var client = await queueClientProvider.GetQueueClientAsync("connections-established", logger);

            var ports = Port.Split(':');
            var destinationPort = int.Parse(ports[0]);
            var sourcePort = int.Parse(ports[1]);

            var connectionInfo = new ConnectionDetails
            {
                WorkspaceId = WorkspaceId,
                SourcePort = sourcePort,
                DestinationPort = destinationPort,
                AgentName = AgentName,
                AgentUid = AgentUid,
            };

            var message = new Message(JsonSerializer.SerializeToUtf8Bytes(
                connectionInfo,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
            {
                SessionId = connectionInfo.GetMessagingSessionId(),
                Label = MessageLabels.ConnectionEstablished,
            };

            await client.SendAsync(message);
        }
    }
}
