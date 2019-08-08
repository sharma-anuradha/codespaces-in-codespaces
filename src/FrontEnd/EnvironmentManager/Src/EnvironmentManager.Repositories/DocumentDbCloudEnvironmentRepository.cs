// <copyright file="DocumentDbCloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories
{
    /// <summary>
    /// A document repository of <see cref="CloudEnvironment"/>.
    /// </summary>
    [DocumentDbCollectionId(CloudEnvironmentsCollectionId)]
    public class DocumentDbCloudEnvironmentRepository
        : DocumentDbCollection<CloudEnvironment>, ICloudEnvironmentRepository
    {
        /// <summary>
        /// The models collection id.
        /// </summary>
        public const string CloudEnvironmentsCollectionId = "cloud_environments";

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentDbCloudEnvironmentRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public DocumentDbCloudEnvironmentRepository(
                IOptions<DocumentDbCollectionOptions> options,
                IDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(options.PromoteToOptionSnapshot(), clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
        }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        public static void ConfigureOptions(DocumentDbCollectionOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.PartitioningStrategy = PartitioningStrategy.IdOnly;
        }

        /// <inheritdoc/>
        public override Task<CloudEnvironment> CreateOrUpdateAsync(
            [ValidatedNotNull] CloudEnvironment document,
            [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            Requires.NotNull(document, nameof(document));
            Requires.NotNull(logger, nameof(logger));

            // TODO: ADD option to SDK
            // bool AutoUpdateTimeStamps { get; set; }
            document.Created = DateTime.UtcNow;

            return base.CreateOrUpdateAsync(document, logger);
        }

        /// <inheritdoc/>
        /// <summary>
        /// Updates the model document in the database. The document's `Updated` field is also set to UTC now.
        /// </summary>
        public override Task<CloudEnvironment> UpdateAsync(
            [ValidatedNotNull] CloudEnvironment document,
            [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            Requires.NotNull(document, nameof(document));
            Requires.NotNull(logger, nameof(logger));

            // TODO: ADD option to SDK
            // bool AutoUpdateTimeStamps { get; set; }
            document.Updated = DateTime.UtcNow;

            return base.UpdateAsync(document, logger);
        }
    }
}
