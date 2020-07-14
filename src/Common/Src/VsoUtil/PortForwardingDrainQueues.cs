// <copyright file="PortForwardingDrainQueues.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
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
    /// The pf-drain verb.
    /// </summary>
    [Verb("pf-drain", HelpText = "Drain port forwarding queues.")]
    public class PortForwardingDrainQueues : CommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether to drain new connections queue.
        /// </summary>
        [Option('n', "new", HelpText = "Drain new connections queue")]
        public bool DrainNewConnections { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to drain established connections queue.
        /// Note: e was already taken by env.
        /// </summary>
        [Option('c', "established", HelpText = "Drain established connections queue")]
        public bool DrainEstablishedConnections { get; set; }

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

            if (DrainNewConnections)
            {
                stdout.WriteLine("Draining new connections queue.");
                var newSessions = await queueClientProvider.GetSessionClientAsync(QueueNames.NewConnections, logger);
                await DrainQueue(stdout, stderr, newSessions);
            }

            if (DrainEstablishedConnections)
            {
                stdout.WriteLine("Draining established connections queue.");
                var establishedSessions = await queueClientProvider.GetSessionClientAsync(QueueNames.EstablishedConnections, logger);
                await DrainQueue(stdout, stderr, establishedSessions);
            }
        }

        private async Task DrainQueue(TextWriter stdout, TextWriter stderr, ISessionClient client)
        {
            var session = await TryGetValueAsync(() => client.AcceptMessageSessionAsync(TimeSpan.FromSeconds(1)));
            while (session != null)
            {
                var message = await TryGetValueAsync(() => session.ReceiveAsync(TimeSpan.FromSeconds(1)));
                while (message != null)
                {
                    stdout.WriteLine("Draining message for session: {0}", message.SessionId);

                    await session.CompleteAsync(message.SystemProperties.LockToken);
                    message = await TryGetValueAsync(() => session.ReceiveAsync(TimeSpan.FromSeconds(1)));
                }

                await session.CloseAsync();
                session = await TryGetValueAsync(() => client.AcceptMessageSessionAsync(TimeSpan.FromSeconds(1)));
            }
        }

        private async Task<T> TryGetValueAsync<T>(Func<Task<T>> func)
        {
            try
            {
                return await func();
            }
            catch
            {
                return default;
            }
        }
    }
}
