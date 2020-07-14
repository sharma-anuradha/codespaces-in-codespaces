// <copyright file="CreatePortForwardingConnection.cs" company="Microsoft">
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
    [Verb("pf-connection", HelpText = "Create new port forwarding connection.")]
    public class CreatePortForwardingConnection : CommandBase
    {
        /// <summary>
        /// Gets or sets connection workspace id.
        /// </summary>
        [Option('w', "workspace", HelpText = "Workspace Id")]
        public string WorkspaceId { get; set; }

        /// <summary>
        /// Gets or sets connection source port.
        /// </summary>
        [Option('p', "port", HelpText = "Port")]
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets connection token.
        /// </summary>
        [Option('t', "token", HelpText = "Token")]
        public string Token { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteCommandAsync(services, stdout, stderr).Wait();
        }

        private async Task ExecuteCommandAsync(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var queueClientProvider = services.GetService<IServiceBusClientProvider>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logger = loggerFactory.New();
            var client = await queueClientProvider.GetQueueClientAsync("connections-new", logger);

            var connectionInfo = new ConnectionRequest
            {
                WorkspaceId = WorkspaceId,
                Port = Port,
                Token = Token,
            };
            var message = new Message(JsonSerializer.SerializeToUtf8Bytes(connectionInfo, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })) { SessionId = connectionInfo.WorkspaceId };

            await client.SendAsync(message);
        }
    }
}
