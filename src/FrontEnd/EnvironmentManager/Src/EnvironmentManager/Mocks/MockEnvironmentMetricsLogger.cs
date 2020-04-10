// <copyright file="MockEnvironmentMetricsLogger.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks
{
    /// <summary>
    /// The mock cloud environment manager.
    /// </summary>
    public class MockEnvironmentMetricsLogger : IEnvironmentMetricsManager
    {
        /// <inheritdoc/>
        public void PostEnvironmentEvent(CloudEnvironment environment, CloudEnvironmentStateSnapshot lastState, IDiagnosticsLogger logger)
        {
        }

        /// <inheritdoc/>
        public void PostEnvironmentCount(CloudEnvironmentDimensions cloudEnvironmentDimensions, int count, IDiagnosticsLogger logger)
        {
        }
    }
}
