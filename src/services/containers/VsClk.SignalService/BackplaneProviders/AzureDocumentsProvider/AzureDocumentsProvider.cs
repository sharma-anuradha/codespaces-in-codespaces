using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.PartitionManagement;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements IScaleServiceProvider based on an Azure Cosmos database
    /// </summary>
    public class AzureDocumentsProvider : DocumentDatabaseProvider, IBackplaneProvider
    {
        public static readonly string DatabaseId = "presenceService";

        private static readonly string ServiceCollectionId = "services";
        private static readonly string ContactDataCollectionId = "contactsData";
        private static readonly string MessageCollectionId = "messages";
        private static readonly string LeaseCollectionBaseId = "leases-";


        // RU values for production
        private const int ServicesRUThroughput = 400;
        private const int ContactsRUThroughput = 5000;
        private const int MessageRUThroughput = 2000;
        private const int LeasesRUThroughput = 1000;

        // RU values for Development
        private const int ServicesRUThroughput_Dev = 400;
        private const int ContactsRUThroughput_Dev = 400;
        private const int MessageRUThroughput_Dev = 400;
        private const int LeasesRUThroughput_Dev = 400;

        private readonly DatabaseSettings databaseSettings;
        private readonly List<IChangeFeedProcessor> feedProcessors = new List<IChangeFeedProcessor>();
        private DocumentCollection providerLeaseColletion;

        private AzureDocumentsProvider(
            DatabaseSettings databaseSettings,
            ILogger<AzureDocumentsProvider> logger,
            IFormatProvider formatProvider)
            : base(logger, formatProvider)
        {
            this.databaseSettings = Requires.NotNull(databaseSettings, nameof(databaseSettings));
            Requires.NotNullOrEmpty(databaseSettings.EndpointUrl, nameof(databaseSettings.EndpointUrl));
            Requires.NotNullOrEmpty(databaseSettings.AuthorizationKey, nameof(databaseSettings.AuthorizationKey));

            // initialize document client
            Client = new DocumentClient(new Uri(databaseSettings.EndpointUrl), databaseSettings.AuthorizationKey,
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                });
        }

        public static async Task<AzureDocumentsProvider> CreateAsync(
            string serviceId,
            DatabaseSettings databaseSettings,
            ILogger<AzureDocumentsProvider> logger,
            IFormatProvider formatProvider,
            bool deleteDabatase = false)
        {
            var databaseBackplaneProvider = new AzureDocumentsProvider(databaseSettings, logger, formatProvider);

            if (deleteDabatase)
            {
                try
                {
                    await databaseBackplaneProvider.Client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(AzureDocumentsProvider.DatabaseId));
                }
                catch (DocumentClientException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // the database was not found
                }
            }

            await databaseBackplaneProvider.InitializeAsync(serviceId, databaseSettings.IsProduction);
            return databaseBackplaneProvider;
        }

        public DocumentClient Client { get; }

        #region Callback from the Feed processor

        private Task OnServiceDocumentsChangedAsync(IReadOnlyList<Document> docs)
        {
            return OnServiceDocumentsChangedAsync(docs.Select(d => new AzureDocument(d)).ToList());
        }

        private Task OnContactDocumentsChangedAsync(IReadOnlyList<Document> docs)
        {
            return OnContactDocumentsChangedAsync(docs.Select(d => new AzureDocument(d)).ToList());
        }

        private Task OnMessageDocumentsChangedAsync(IReadOnlyList<Document> docs)
        {
            return OnMessageDocumentsChangedAsync(docs.Select(d => new AzureDocument(d)).ToList());
        }

        #endregion

        protected override async Task DisposeInternalAsync()
        {
            foreach (var feedProcessor in this.feedProcessors)
            {
                await feedProcessor.StopAsync();
            }

            await Client.DeleteDocumentCollectionAsync(this.providerLeaseColletion.SelfLink);
            Client.Dispose();
        }

        protected override async Task<List<ContactDataDocument>> GetContactsDataByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var queryable = Client.CreateDocumentQuery<ContactDataDocument>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, ContactDataCollectionId))
                .Where(c => c.Email == email);

            var matchContacts = await ToListAsync(queryable, cancellationToken);
            return matchContacts;
        }

        protected override async Task UpsertServiceDocumentAsync(ServiceDocument serviceDocument, CancellationToken cancellationToken)
        {
            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, ServiceCollectionId);
            await Client.UpsertDocumentAsync(documentCollectionUri, serviceDocument, cancellationToken: cancellationToken);
        }

        protected override async Task InsertMessageDocumentAsync(MessageDocument messageDocument, CancellationToken cancellationToken)
        {
            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, MessageCollectionId);
            await Client.CreateDocumentAsync(documentCollectionUri, messageDocument, cancellationToken: cancellationToken);
        }

        protected override async Task<TimeSpan> UpsertContactDocumentAsync(ContactDataDocument contactDocument, CancellationToken cancellationToken)
        {
            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, ContactDataCollectionId);
            var resonse = await Client.UpsertDocumentAsync(documentCollectionUri, contactDocument, cancellationToken: cancellationToken);
            return resonse.RequestLatency;
        }

        protected override async Task<(ContactDataDocument, TimeSpan)> GetContactDataDocumentAsync(string contactId, CancellationToken cancellationToken)
        {
            try
            {
                var documentUri = UriFactory.CreateDocumentUri(DatabaseId, ContactDataCollectionId, contactId);
                var response = await Client.ReadDocumentAsync<ContactDataDocument>(documentUri, cancellationToken: cancellationToken);
                return (response.Document, response.RequestLatency);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return (null, TimeSpan.FromMilliseconds(0));
            }
        }

        protected override async Task<List<ServiceDocument>> GetServiceDocuments(CancellationToken cancellationToken)
        {
            var queryable = Client.CreateDocumentQuery<ServiceDocument>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, ServiceCollectionId));

            return await ToListAsync(queryable, cancellationToken);
        }

        protected override Task DeleteServiceDocumentById(string serviceId, CancellationToken cancellationToken)
        {
            var documentUri = UriFactory.CreateDocumentUri(DatabaseId, ServiceCollectionId, serviceId);
            return Client.DeleteDocumentAsync(documentUri, cancellationToken: cancellationToken);
        }


        /// <summary>
        /// Create a change feed processor based on a collection id
        /// </summary>
        /// <param name="collectionId">The target collection id</param>
        /// <param name="leaseCollectionId">The lease collection id to use</param>
        /// <param name="hostName">Reference host name</param>
        /// <param name="onDocumentsChanged">Callback</param>
        /// <returns></returns>
        private async Task<IChangeFeedProcessor> CreateChangeFeedProcessorAsync(
            string collectionId,
            string leaseCollectionId,
            string hostName,
            Func<IReadOnlyList<Document>, Task> onDocumentsChanged)
        {
            Logger.LogInformation($"CreateChangeFeedProcessorAsync collectionId:{collectionId} hostName:{hostName}");

            var feedCollectionInfo = new DocumentCollectionInfo()
            {
                DatabaseName = DatabaseId,
                CollectionName = collectionId,
                Uri = new Uri(this.databaseSettings.EndpointUrl),
                MasterKey = this.databaseSettings.AuthorizationKey
            };

            var leaseCollectionInfo = new DocumentCollectionInfo()
            {
                DatabaseName = DatabaseId,
                CollectionName = leaseCollectionId,
                Uri = new Uri(this.databaseSettings.EndpointUrl),
                MasterKey = this.databaseSettings.AuthorizationKey
            };

            var builder = new ChangeFeedProcessorBuilder();
            var feedProcessor = await builder
                .WithHostName(hostName)
                .WithFeedCollection(feedCollectionInfo)
                .WithLeaseCollection(leaseCollectionInfo)
                .WithObserverFactory(new CallbackFeedObserverFactory(hostName, onDocumentsChanged, Logger))
                .BuildAsync();

            await feedProcessor.StartAsync();
            return feedProcessor;
        }

        private async Task InitializeAsync(string serviceId, bool isProduction)
        {
            Logger.LogInformation($"Creating database:{DatabaseId} if not exists");

            // Create the database
            var databaseResponse = await Client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId });

            // Create 'services'
            Logger.LogInformation($"Creating Collection:{ServiceCollectionId}");
            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(ServiceCollectionId),
                new RequestOptions
                {
                    OfferThroughput = isProduction ? ServicesRUThroughput : ServicesRUThroughput_Dev
                });

            // Ensure we create an entry that backup our leases collection
            await InitializeServiceIdAsync(serviceId);

            // Create 'contacts'
            Logger.LogInformation($"Creating Collection:{ContactDataCollectionId}");
            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(ContactDataCollectionId),
                new RequestOptions
                {
                    OfferThroughput = isProduction ? ContactsRUThroughput : ContactsRUThroughput_Dev
                });

            // Create 'message'
            Logger.LogInformation($"Creating Collection:{MessageCollectionId}");
            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(MessageCollectionId),
                new RequestOptions
                {
                    OfferThroughput = isProduction ? MessageRUThroughput: MessageRUThroughput_Dev
                });

            // cleanup all 'stale' leases collections
            var servicesIds = (await GetServiceDocuments(CancellationToken.None)).Select(d => d.Id);
            var staleLeaseCollections = Client.CreateDocumentCollectionQuery(databaseResponse.Resource.SelfLink)
                .ToList()
                .Where(d => d.Id.StartsWith(LeaseCollectionBaseId) && !servicesIds.Contains(d.Id.Substring(LeaseCollectionBaseId.Length)));

            foreach(var docCollection in staleLeaseCollections)
            {
                try
                {
                    Logger.LogInformation($"Delete stale lease collection:{docCollection.Id}");
                    await Client.DeleteDocumentCollectionAsync(docCollection.SelfLink);
                }
                catch(Exception error)
                {
                    Logger.LogError(error, $"Failed to delete stale lease collection:{docCollection.Id}");
                }
            }

            var leaseCollectionId = $"{LeaseCollectionBaseId}{serviceId}";
            // Create 'leases'
            Logger.LogInformation($"Creating Collection:{leaseCollectionId}");
            this.providerLeaseColletion = (await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(leaseCollectionId),
                new RequestOptions
                {
                    OfferThroughput = isProduction ? LeasesRUThroughput : LeasesRUThroughput_Dev
                })).Resource;

            // Create 'services' processor
            this.feedProcessors.Add(await CreateChangeFeedProcessorAsync(
                ServiceCollectionId,
                leaseCollectionId,
                "PresenceServiceR-Services",
                OnServiceDocumentsChangedAsync));

            // Create 'contacts' processor
            this.feedProcessors.Add(await CreateChangeFeedProcessorAsync(
                ContactDataCollectionId,
                leaseCollectionId,
                "PresenceServiceR-Contacts",
                OnContactDocumentsChangedAsync));

            // Create 'message' processor
            this.feedProcessors.Add(await CreateChangeFeedProcessorAsync(
                MessageCollectionId,
                leaseCollectionId,
                "PresenceServiceR-Messages",
                OnMessageDocumentsChangedAsync));
        }

        private static DocumentCollection CreateDocumentCollectionDefinition(string resourceId)
        {
            var collectionDefinition = new DocumentCollection();
            collectionDefinition.Id = resourceId;
            collectionDefinition.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
            return collectionDefinition;
        }

        private static async Task<List<T>> ToListAsync<T>(IDocumentQuery<T> queryable, CancellationToken token)
        {
            var list = new List<T>();
            while (queryable.HasMoreResults)
            {   //Note that ExecuteNextAsync can return many records in each call
                var response = await queryable.ExecuteNextAsync<T>(token);
                list.AddRange(response);
            }

            return list;
        }

        private static Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken token)
        {
            return ToListAsync(query.AsDocumentQuery(), token);
        }

        private class AzureDocument : IDocument
        {
            private Document azureDocument;

            public AzureDocument(Document azureDocument)
            {
                this.azureDocument = azureDocument;
            }

            public string Id => this.azureDocument.Id;

            public async Task<T> ReadAsAsync<T>()
            {
                using (var ms = new MemoryStream())
                using (var reader = new StreamReader(ms))
                {
                    this.azureDocument.SaveTo(ms);
                    ms.Position = 0;
                    return JsonConvert.DeserializeObject<T>(await reader.ReadToEndAsync());
                }
            }
        }
    }
}
