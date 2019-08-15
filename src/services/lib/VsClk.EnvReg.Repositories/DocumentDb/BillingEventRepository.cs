using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.Util;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories.DocumentDb
{
    [DocumentDbCollectionId(EventCollectionId)]
    public class DocumentDbBillingEventRepository : DocumentDbCollection<BillingEvent>, IBillingEventRepository
    {
        public const string EventCollectionId = "environment_billing_events";

        public DocumentDbBillingEventRepository(
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
            options.PartitioningStrategy = PartitioningStrategy.Custom;
            options.CustomPartitionKeyPaths = new[]
            {
                // Billing events are partitioned by subscription ID. Most queries
                // will filter on a specific account, which includes a subscription ID.
                "/account/subscription",
            };
            options.CustomPartitionKeyFunc = (entity) =>
            {
                return new PartitionKey(((BillingEvent)entity).Account?.Subscription);
            };
        }
    }
}
