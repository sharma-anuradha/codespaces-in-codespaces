// <copyright file="BaseBackgroundTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Defines a base background task class.
    /// </summary>
    public abstract class BaseBackgroundTask : IBackgroundTask
    {
        /// <summary>
        /// The default job enabled state.
        /// </summary>
        private const bool DefaultJobEnabledState = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseBackgroundTask"/> class.
        /// </summary>
        /// <param name="configurationReader">Configuration reader.</param>
        public BaseBackgroundTask(IConfigurationReader configurationReader)
        {
            ConfigurationReader = configurationReader;
        }            

        /// <summary>
        /// Gets the base configuration name or component name.
        /// </summary>
        protected abstract string ConfigurationBaseName { get; }

        protected IConfigurationReader ConfigurationReader { get; }

        /// <inheritdoc/>
        public abstract void Dispose();

        /// <inheritdoc/>
        public async Task<bool> RunTaskAsync(TimeSpan taskInterval, IDiagnosticsLogger logger)
        {
            var isEnabled = await ConfigurationReader.ReadSettingAsync(ConfigurationBaseName, ConfigurationConstants.EnabledSettingName, logger, DefaultJobEnabledState);
            logger.FluentAddValue("ConfigurationIsEnabledValue", isEnabled);
            return await (isEnabled ? RunAsync(taskInterval, logger) : Task.FromResult(true));
        }

        /// <summary>
        /// Core task which is executed.
        /// </summary>
        /// <param name="taskInterval">The interval (frequency) at which the task should be executed.</param>
        /// <param name="logger">The logger to use during task execution.</param>
        /// <returns>Whether the task should run again.</returns>
        /// <remarks>The <paramref name="taskInterval"/> is used to determine whether or not the task has real work to do.
        /// It's the responsibility of the task to retrieve a <see cref="ClaimedDistributedLease"/> and verify whether
        /// it should execute.</remarks>
        protected abstract Task<bool> RunAsync(TimeSpan taskInterval, IDiagnosticsLogger logger);
    }
}
