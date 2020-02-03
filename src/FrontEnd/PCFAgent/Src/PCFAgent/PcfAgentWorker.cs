// <copyright file="PcfAgentWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.PrivacyServices.CommandFeed.Client;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PcfAgent
{
    /// <summary>
    /// Worker that runs PCF Agent.
    /// </summary>
    [LoggingBaseName("pcf_agent_worker")]
    public class PcfAgentWorker : BackgroundService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PcfAgentWorker"/> class.
        /// </summary>
        /// <param name="commandFeedClient">Command Feed Client.</param>
        /// <param name="dataAgent">Privacy Data Agent.</param>
        /// <param name="commandFeedLogger">Command Feed Logger.</param>
        /// <param name="logger">VsSaas Logger.</param>
        public PcfAgentWorker(ICommandFeedClient commandFeedClient, IPrivacyDataAgent dataAgent, CommandFeedLogger commandFeedLogger, IDiagnosticsLogger logger)
        {
            CommandFeedClient = commandFeedClient;
            DataAgent = dataAgent;
            CommandFeedLogger = commandFeedLogger;
            Logger = logger;
        }

        private ICommandFeedClient CommandFeedClient { get; }

        private IPrivacyDataAgent DataAgent { get; }

        private CommandFeedLogger CommandFeedLogger { get; }

        private IDiagnosticsLogger Logger { get; }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInfo(GetType().FormatLogMessage(nameof(ExecuteAsync)));

            PrivacyCommandReceiver receiver = new PrivacyCommandReceiver(
                   DataAgent,
                   CommandFeedClient,
                   CommandFeedLogger);

            await receiver.BeginReceivingAsync(stoppingToken);
        }
    }
}
