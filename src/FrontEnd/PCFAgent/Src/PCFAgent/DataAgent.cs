// <copyright file="DataAgent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.PrivacyServices.CommandFeed.Client;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PcfAgent
{
    /// <summary>
    /// Privacy Data Agent Implementation for VSO.
    /// </summary>
    [LoggingBaseName("pcf_data_agent")]
    public class DataAgent : IPrivacyDataAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataAgent"/> class.
        /// </summary>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        public DataAgent(IDiagnosticsLogger logger)
        {
            Logger = logger;
        }

        private IDiagnosticsLogger Logger { get; }

        /// <inheritdoc />
        public Task ProcessAccountClosedAsync(IAccountCloseCommand command)
        {
            Logger.LogInfo(GetType().FormatLogMessage(nameof(ProcessAccountClosedAsync)));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ProcessAgeOutAsync(IAgeOutCommand command)
        {
            Logger.LogInfo(GetType().FormatLogMessage(nameof(ProcessAgeOutAsync)));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ProcessDeleteAsync(IDeleteCommand command)
        {
            Logger.LogInfo(GetType().FormatLogMessage(nameof(ProcessDeleteAsync)));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ProcessExportAsync(IExportCommand command)
        {
            Logger.LogInfo(GetType().FormatLogMessage(nameof(ProcessExportAsync)));
            return Task.CompletedTask;
        }
    }
}
