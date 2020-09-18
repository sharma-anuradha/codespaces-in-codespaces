// <copyright file="DocumentDbCloudEnvironmentHeartbeatRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository
{
    /// <summary>
    /// A document repository of <see cref="CloudEnvironmentHeartbeat"/>.
    /// </summary>
    [DocumentDbCollectionId(CloudEnvironmentsHeartbeatCollectionId)]
    public class DocumentDbCloudEnvironmentHeartbeatRepository
        : DocumentDbCollection<CloudEnvironmentHeartbeat>, ICloudEnvironmentHeartbeatRepository
    {
        /// <summary>
        /// The models collection id.
        /// </summary>
        public const string CloudEnvironmentsHeartbeatCollectionId = "cloud_environments_heartbeat";

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentDbCloudEnvironmentHeartbeatRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="controlPlaneInfo">The control-plane information.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        /// <param name="environmentManagerSettings">The Environment Manager Settings.</param>
        public DocumentDbCloudEnvironmentHeartbeatRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IRegionalDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues,
                EnvironmentManagerSettings environmentManagerSettings)
            : base(
                  options,
                  clientProvider,
                  healthProvider,
                  loggerFactory,
                  defaultLogValues)
        {
        }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        /// <remarks>
        /// Keep this in sync with <see cref="CloudEnvironmentCosmosContainer.ConfigureOptions"/>.
        /// </remarks>
        public static void ConfigureOptions(DocumentDbCollectionOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.PartitioningStrategy = PartitioningStrategy.IdOnly;
        }
    }
}
