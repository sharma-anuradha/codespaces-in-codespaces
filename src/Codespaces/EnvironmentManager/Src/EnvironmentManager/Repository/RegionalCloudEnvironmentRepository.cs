// <copyright file="RegionalCloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;

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
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public RegionalCloudEnvironmentRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IRegionalDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(
                  options,
                  clientProvider,
                  healthProvider,
                  loggerFactory,
                  defaultLogValues)
        {
        }
    }
}
