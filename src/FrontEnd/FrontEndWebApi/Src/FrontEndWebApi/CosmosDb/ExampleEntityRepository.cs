// <copyright file="ExampleEntityRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.CosmosDb
{
    /// <summary>
    /// A document repository of <see cref="ExampleEntity"/>.
    /// </summary>
    [DocumentDbCollectionId(CollectionName)]
    public class ExampleEntityRepository : DocumentDbCollection<ExampleEntity>
    {
        /// <summary>
        /// The name of the collection in CosmosDB.
        /// </summary>
        public const string CollectionName = "exampleEntities";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleEntityRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public ExampleEntityRepository(
                IOptionsSnapshot<DocumentDbCollectionOptions> options,
                IDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(options, clientProvider, healthProvider, loggerFactory, defaultLogValues)
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
            options.LogPreconditionFailedErrorsAsWarnings = true;
        }
    }
}
