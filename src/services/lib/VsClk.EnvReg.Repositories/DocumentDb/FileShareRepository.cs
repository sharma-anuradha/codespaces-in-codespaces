﻿using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.Util;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories.DocumentDb
{
    [DocumentDbCollectionId(FileShareCollectionId)]
    public class DocumentDbSotrageRegistrationRepository : DocumentDbCollection<FileShare>, IStorageRegistrationRepository
    {
        public const string FileShareCollectionId = "environment_file_shares";

        public DocumentDbSotrageRegistrationRepository(
            IOptions<DocumentDbCollectionOptions> collectionOptions,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(collectionOptions.PromoteToOptionSnapshot(), clientProvider, healthProvider, loggerFactory, defaultLogValues)
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
    }
}
