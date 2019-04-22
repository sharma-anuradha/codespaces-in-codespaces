using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.Util;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories.DocumentDb
{
    /// <summary>
    /// A document repository of <see cref="EnvironmentRegistration"/>.
    /// </summary>
    [DocumentDbCollectionId(EnvironmentRegistrationCollectionId)]
    public class DocumentDbEnvironmentRegistrationRepository
        : DocumentDbCollection<EnvironmentRegistration>, IEnvironmentRegistrationRepository
    {
        /// <summary>
        /// The models collection id.
        /// </summary>
        public const string EnvironmentRegistrationCollectionId = "environment_registrations";

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentDbEnvironmentRegistrationRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public DocumentDbEnvironmentRegistrationRepository(
                IOptions<DocumentDbCollectionOptions> options,
                IDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                LogValueSet defaultLogValues)
            : base(options.PromoteToOptionSnapshot(), clientProvider, healthProvider, defaultLogValues)
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

        public override Task<EnvironmentRegistration> CreateOrUpdateAsync([ValidatedNotNull] EnvironmentRegistration document, [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            document.Created = DateTime.UtcNow;
            return base.CreateOrUpdateAsync(document, logger);
        }

        /// <summary>
        /// Updates the model document in the database. The document's `Updated` field is also set to UTC now.
        /// </summary>
        /// <param name="document">The model to update.</param>
        /// <param name="logger">The diagnostics logger instance to use.</param>
        /// <returns>The updated model.</returns>
        public override Task<EnvironmentRegistration> UpdateAsync([ValidatedNotNull] EnvironmentRegistration document, [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            document.Updated = DateTime.UtcNow;
            return base.UpdateAsync(document, logger);
        }

        /// <summary>
        /// Gets a fully-materialized list of IDs from the models collection in the database. The `where` filter is applied to
        /// the query.
        /// </summary>
        /// <param name="where">The filter to apply to the query.</param>
        /// <param name="logger">The diagnostics logger instance to use.</param>
        /// <param name="pageResultsCallback">An optional callback to execute after each page is returned. Provides the values
        /// that were contained in the current page of results, and should return an awaitable task representing the completion
        /// the page processing.</param>
        /// <returns>An in-memory list of strings where each string is an ID of an entity that's present in the collection.</returns>
        public async Task<IEnumerable<string>> GetWhereAsync(
            Expression<Func<EnvironmentRegistration, bool>> where,
            IDiagnosticsLogger logger,
            Func<IEnumerable<string>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            Requires.NotNull(logger, nameof(logger));

            var result = new List<string>();

            try
            {
                var duration = logger.StartDuration();

                Uri uri = CreateDocumentCollectionUri();
                var client = await GetClientAsync();
                var query = client.CreateDocumentQuery<EnvironmentRegistration>(
                        uri,
                        new FeedOptions { EnableCrossPartitionQuery = true, MaxDegreeOfParallelism = -1, EnableScanInQuery = true })
                    .Where(where)
                    .AsDocumentQuery();

                while (query.HasMoreResults)
                {
                    var executeNextResponse = await query.ExecuteNextAsync<string>();
                    result.AddRange(executeNextResponse);

                    logger.AddDuration(duration)
                    .AddRequestCharge(executeNextResponse.RequestCharge)
                    .LogInfo($"docdb_{LoggingDocumentName}_getidswhere");

                    // Invoke optional callback if provided
                    if (pageResultsCallback != null)
                    {
                        await pageResultsCallback(executeNextResponse, logger);
                    }

                    // Reset duration for the next page of results so that we record duration for each DB operation separately
                    duration = logger.StartDuration();
                }
            }
            catch (Exception e)
            {
                logger.LogException($"docdb_{LoggingDocumentName}_error", e);
                throw;
            }

            return result;
        }
    }
}
