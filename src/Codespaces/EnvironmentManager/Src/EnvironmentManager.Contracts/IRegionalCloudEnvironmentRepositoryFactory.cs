// <copyright file="IRegionalCloudEnvironmentRepositoryFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A factory used to create <see cref="IRegionalCloudEnvironmentRepository"/> instances.
    /// </summary>
    public interface IRegionalCloudEnvironmentRepositoryFactory
    {
        /// <summary>
        /// Gets the regional cloud environment repository for the specificed control-plane location.
        /// </summary>
        /// <param name="controlPlaneLocation">The control-plane location.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The regional cloud environment repository.</returns>
        IRegionalCloudEnvironmentRepository GetRegionalRepository(AzureLocation controlPlaneLocation, IDiagnosticsLogger logger);
    }
}
