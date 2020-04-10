// <copyright file="IEnvironmentMetricsManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Metrics logger for <see cref="CloudEnvironment"/>s.
    /// </summary>
    public interface IEnvironmentMetricsManager
    {
        /// <summary>
        /// Post an environment event to the metrics logger.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        /// <param name="lastState">The snapshot of the prior cloud environment state.</param>
        /// <param name="logger">The diagnostics logger.</param>
        void PostEnvironmentEvent(CloudEnvironment cloudEnvironment, CloudEnvironmentStateSnapshot lastState, IDiagnosticsLogger logger);

        /// <summary>
        /// Post the aggregate count of environments over <see cref="CloudEnvironmentDimensions"/>.
        /// </summary>
        /// <param name="cloudEnvironmentDimensions">The dimensions.</param>
        /// <param name="count">The count.</param>
        /// <param name="logger">The diagnostics logger.</param>
        void PostEnvironmentCount(CloudEnvironmentDimensions cloudEnvironmentDimensions, int count, IDiagnosticsLogger logger);
    }
}
