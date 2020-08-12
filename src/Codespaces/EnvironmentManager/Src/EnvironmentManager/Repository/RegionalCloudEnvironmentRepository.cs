// <copyright file="RegionalCloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository
{
    /// <summary>
    /// A regional document repository of <see cref="CloudEnvironment"/>.
    /// </summary>
    public class RegionalCloudEnvironmentRepository : DocumentDbCloudEnvironmentRepository, IRegionalCloudEnvironmentRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegionalCloudEnvironmentRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="controlPlaneInfo">The control-plane information.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        /// <param name="environmentManagerSettings">The Environment Manager settings.</param>
        public RegionalCloudEnvironmentRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IRegionalDocumentDbClientProvider clientProvider,
                IControlPlaneInfo controlPlaneInfo,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                EnvironmentManagerSettings environmentManagerSettings,
                LogValueSet defaultLogValues)
            : base(
                  options,
                  clientProvider,
                  controlPlaneInfo,
                  healthProvider,
                  loggerFactory,
                  defaultLogValues,
                  environmentManagerSettings)
        {
        }
    }
}
