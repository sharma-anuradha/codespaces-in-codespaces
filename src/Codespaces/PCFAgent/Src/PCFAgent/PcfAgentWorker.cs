// <copyright file="PcfAgentWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.PrivacyServices.CommandFeed.Client;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PcfAgent
{
    /// <summary>
    /// Worker that runs PCF Agent.
    /// </summary>
    [LoggingBaseName("pcf_agent_worker")]
    public class PcfAgentWorker : BackgroundService
    {
        private const string PcfAgentOverrideKey = "pcf:enable";

        /// <summary>
        /// Initializes a new instance of the <see cref="PcfAgentWorker"/> class.
        /// </summary>
        /// <param name="commandFeedClient">Command Feed Client.</param>
        /// <param name="dataAgent">Privacy Data Agent.</param>
        /// <param name="commandFeedLogger">Command Feed Logger.</param>
        /// <param name="logger">VsSaas Logger.</param>
        /// <param name="systemConfiguration">System Configuration.</param>
        public PcfAgentWorker(ICommandFeedClient commandFeedClient, IPrivacyDataAgent dataAgent, CommandFeedLogger commandFeedLogger, IDiagnosticsLogger logger, ISystemConfiguration systemConfiguration)
        {
            CommandFeedClient = commandFeedClient;
            DataAgent = dataAgent;
            CommandFeedLogger = commandFeedLogger;
            Logger = logger;
            SystemConfiguration = systemConfiguration;
        }

        private ICommandFeedClient CommandFeedClient { get; }

        private IPrivacyDataAgent DataAgent { get; }

        private CommandFeedLogger CommandFeedLogger { get; }

        private IDiagnosticsLogger Logger { get; }

        private ISystemConfiguration SystemConfiguration { get; }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInfo(GetType().FormatLogMessage(nameof(ExecuteAsync)));

            var isEnabled = false;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            Logger.FluentAddValue("IsPcfAgentEnabled", isEnabled)
                        .LogInfo(GetType().FormatLogMessage("pcf_status_check"));

            while (!stoppingToken.IsCancellationRequested)
            {
                // Defaults to true if no systemConfig record exists.
                var enabledInSystemConfig = await SystemConfiguration.GetValueAsync(PcfAgentOverrideKey, Logger.NewChildLogger(), true);

                if (enabledInSystemConfig != isEnabled)
                {
                    if (enabledInSystemConfig)
                    {
                        // If the current cancellationToken is previously cancelled, create a new one to use with PCF.
                        if (cts.Token.IsCancellationRequested)
                        {
                            cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        }

                        PrivacyCommandReceiver receiver = new PrivacyCommandReceiver(
                        DataAgent,
                        CommandFeedClient,
                        CommandFeedLogger);
                        _ = receiver.BeginReceivingAsync(cts.Token);
                    }
                    else
                    {
                        cts.Cancel();
                    }

                    isEnabled = enabledInSystemConfig;

                    Logger.FluentAddValue("IsPcfAgentEnabled", isEnabled)
                        .LogInfo(GetType().FormatLogMessage("pcf_status_check"));
                }

                // Check the system config record every minute, to determine whether the PCF processing needs to be disabled.
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
    }
}
