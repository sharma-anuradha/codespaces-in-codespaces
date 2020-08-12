// <copyright file="IEnvironmentArchivalTimeCalculator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Computes the archival time for a specific environment.
    /// </summary>
    public interface IEnvironmentArchivalTimeCalculator
    {
        /// <summary>
        /// Computes the hours until an enviornment should be archived.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The archival time in hours.</returns>
        public Task<double> ComputeHoursToArchival(CloudEnvironment environment, IDiagnosticsLogger logger);
    }
}
