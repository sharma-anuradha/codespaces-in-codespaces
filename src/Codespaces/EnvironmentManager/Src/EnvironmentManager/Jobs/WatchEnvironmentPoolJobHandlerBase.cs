// <copyright file="WatchEnvironmentPoolJobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    /// <summary>
    /// Base class for all our watch pool job handlers.
    /// </summary>
    /// <typeparam name="TJobHandlerType">Type of the job handler type.</typeparam>
    [JobHandlerErrorCallback(typeof(DocumentClientJobHandlerError))]
    public abstract class WatchEnvironmentPoolJobHandlerBase<TJobHandlerType> : JobHandlerPayloadBase<WatchEnvironmentPoolPayloadFactory.EnvironmentPoolPayload<TJobHandlerType>>
        where TJobHandlerType : class
    {
        /// <summary>
        /// Feature flag to control whether the job pools are enabled.
        /// </summary>
        public const string WatchPoolJobsEnabledFeatureFlagName = "WatchEnvironmentPoolJobs";

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchEnvironmentPoolJobHandlerBase{T}"/> class.
        /// </summary>
        /// <param name="poolDefinitionStore">Resource pool definition store.</param>
        /// <param name="configReader">Configuration store.</param>
        public WatchEnvironmentPoolJobHandlerBase(
           IEnvironmentPoolDefinitionStore poolDefinitionStore,
           IConfigurationReader configReader)
           : base(options: JobHandlerOptions.WithValues(1))
        {
            PoolDefinitionStore = Requires.NotNull(poolDefinitionStore, nameof(poolDefinitionStore));
            ConfigReader = Requires.NotNull(configReader, nameof(configReader));
        }

        /// <summary>
        /// Gets the configuration reader.
        /// </summary>
        protected IConfigurationReader ConfigReader { get; }

        /// <summary>
        /// Gets the base logging name.
        /// </summary>
        protected abstract string LogBaseName { get; }

        private IEnvironmentPoolDefinitionStore PoolDefinitionStore { get; }

        /// <summary>
        /// Check feature flag.
        /// </summary>
        /// <param name="featureFlagName">feature flag name.</param>
        /// <param name="logger">logger value.</param>
        /// <param name="defaultValue">default value.</param>
        /// <returns>result.</returns>
        protected Task<bool> IsFeatureFlagEnabledAsync(string featureFlagName, IDiagnosticsLogger logger, bool defaultValue = false)
        {
            return ConfigReader.ReadSettingAsync(featureFlagName, ConfigurationConstants.EnabledSettingName, logger, defaultValue);
        }

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(WatchEnvironmentPoolPayloadFactory.EnvironmentPoolPayload<TJobHandlerType> payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            if (!await IsFeatureFlagEnabledAsync(WatchPoolJobsEnabledFeatureFlagName, logger, false))
            {
                return;
            }

            if (string.IsNullOrEmpty(payload.PoolId))
            {
                throw new NotSupportedException("Pool id == null");
            }

            var pools = await PoolDefinitionStore.RetrieveDefinitionsAsync();
            var pool = pools.FirstOrDefault(r => r.Id == payload.PoolId);
            if (pool == null)
            {
                throw new Exception($"Id:{payload.PoolId} not found on resource pool store");
            }

            await logger.OperationScopeAsync(
                $"{LogBaseName}_run_unit_check",
                (childLogger) => HandleJobAsync(pool, childLogger, cancellationToken));
        }

        /// <summary>
        /// Process the resource pool instance.
        /// </summary>
        /// <param name="pool">Resource pool instance.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        protected abstract Task HandleJobAsync(EnvironmentPool pool, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}
