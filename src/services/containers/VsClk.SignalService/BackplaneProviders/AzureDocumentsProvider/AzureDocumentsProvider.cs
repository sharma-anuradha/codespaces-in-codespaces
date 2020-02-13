// <copyright file="AzureDocumentsProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
    /// Implements IBackplaneProvider based on an Azure Cosmos database.
    /// </summary>
    public class AzureDocumentsProvider : DocumentDatabaseProvider, IContactBackplaneProvider
    {
        public static readonly string DatabaseId = "presenceService";

        // RU values for production
        private const int ServicesRUThroughput = 400;
        private const int ContactsRUThroughput = 5000;
        private const int MessageRUThroughput = 2000;
        private const int LeasesRUThroughput = 1000;

        // RU values for Development
        private const int ServicesRUThroughputDev = 400;
        private const int ContactsRUThroughputDev = 400;
        private const int MessageRUThroughputDev = 400;
        private const int LeasesRUThroughputDev = 400;

        private static readonly string ServiceCollectionId = "services";
        private static readonly string ContactCollectionId = "contacts";
        private static readonly string MessageCollectionId = "messages";
        private static readonly string LeaseCollectionBaseId = "leases-";

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
            Client = new DocumentClient(
                new Uri(databaseSettings.EndpointUrl),
                databaseSettings.AuthorizationKey,
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp,
                });
        }

        public DocumentClient Client { get; }

        public static async Task<AzureDocumentsProvider> CreateAsync(
            (string ServiceId, string Stamp) serviceInfo,
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
                    await databaseBackplaneProvider.Client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
                }
                catch (DocumentClientException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // the database was not found
                }
            }

            await databaseBackplaneProvider.InitializeAsync(serviceInfo, databaseSettings.IsProduction);
            return databaseBackplaneProvider;
        }

        protected override async Task DisposeInternalAsync()
        {
            foreach (var feedProcessor in this.feedProcessors)
            {
                await feedProcessor.StopAsync();
            }

            await Client.DeleteDocumentCollectionAsync(this.providerLeaseColletion.SelfLink);
            Client.Dispose();
        }

        protected override async Task<List<ContactDocument>[]> GetContactsDataByEmailAsync(string[] emails, CancellationToken cancellationToken)
        {
            var results = Enumerable.Repeat(0, emails.Length).Select(i => new List<ContactDocument>()).ToArray();

            var sqlParameters = new SqlParameterCollection();
            int next = 0;
            var emailIndexMap = new Dictionary<string, int>();
            var whereCondition = new StringBuilder();
            foreach (var email in emails)
            {
                if (whereCondition.Length > 0)
                {
                    whereCondition.Append(" Or ");
                }

                var paramName = $"@index{next}";

                whereCondition.Append($"c.email = {paramName}");
                emailIndexMap[email] = next;
                sqlParameters.Add(new SqlParameter(paramName, email));
                ++next;
            }

            var queryable = Client.CreateDocumentQuery<ContactDocument>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, ContactCollectionId), new SqlQuerySpec($"SELECT * FROM c where {whereCondition.ToString()}", sqlParameters));

            var allMatchingContacts = await ToListAsync(queryable, cancellationToken);
            foreach (var item in allMatchingContacts)
            {
                var emailBucket = results[emailIndexMap[item.Email]];
                emailBucket.Add(item);
            }

            return results;
        }

        protected override async Task UpsertServiceDocumentAsync(ServiceDocument serviceDocument, CancellationToken cancellationToken)
        {
            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, ServiceCollectionId);
            await Client.UpsertDocumentAsync(documentCollectionUri, serviceDocument, cancellationToken: cancellationToken);
        }

        protected override async Task InsertMessageDocumentAsync(MessageDocument messageDocument, CancellationToken cancellationToken)
        {
            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, MessageCollectionId);
            var response = await ExecuteWithRetries(() => Client.CreateDocumentAsync(documentCollectionUri, messageDocument, cancellationToken: cancellationToken));
        }

        protected override async Task UpsertContactDocumentAsync(ContactDocument contactDocument, CancellationToken cancellationToken)
        {
            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, ContactCollectionId);
            var resonse = await ExecuteWithRetries(() => Client.UpsertDocumentAsync(documentCollectionUri, contactDocument, cancellationToken: cancellationToken));
        }

        protected override async Task<ContactDocument> GetContactDataDocumentAsync(string contactId, CancellationToken cancellationToken)
        {
            try
            {
                var documentUri = UriFactory.CreateDocumentUri(DatabaseId, ContactCollectionId, contactId);
                var response = await ExecuteWithRetries(() => Client.ReadDocumentAsync<ContactDocument>(documentUri, cancellationToken: cancellationToken));
                return response.Document;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
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

        protected override async Task DeleteMessageDocumentByIds(string[] changeIds, CancellationToken cancellationToken)
        {
            foreach (var changeId in changeIds)
            {
                var documentUri = UriFactory.CreateDocumentUri(DatabaseId, MessageCollectionId, changeId);
                try
                {
                    var response = await ExecuteWithRetries(() => Client.DeleteDocumentAsync(documentUri, cancellationToken: cancellationToken));
                }
                catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
                {
                    // the record was not found
                }
            }
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
            {
                // Note that ExecuteNextAsync can return many records in each call
                var response = await queryable.ExecuteNextAsync<T>(token);
                list.AddRange(response);
            }

            return list;
        }

        private static Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken token)
        {
            return ToListAsync(query.AsDocumentQuery(), token);
        }

#pragma warning disable SA1314 // Type parameter names should begin with T
        private static async Task<V> ExecuteWithRetries<V>(Func<Task<V>> function)
#pragma warning restore SA1314 // Type parameter names should begin with T
        {
            while (true)
            {
                TimeSpan sleepTime;
                try
                {
                    return await function();
                }
                catch (DocumentClientException de)
                {
                    if ((int)de.StatusCode != StatusCodes.Status429TooManyRequests)
                    {
                        throw;
                    }

                    sleepTime = de.RetryAfter;
                }
                catch (AggregateException ae)
                {
                    if (!(ae.InnerException is DocumentClientException ||
                        ae.InnerExceptions.Any(e => e is DocumentClientException)))
                    {
                        throw;
                    }

                    DocumentClientException de = (DocumentClientException)ae.InnerException;
                    if ((int)de.StatusCode != StatusCodes.Status429TooManyRequests)
                    {
                        throw;
                    }

                    sleepTime = de.RetryAfter;
                }

                await Task.Delay(sleepTime);
            }
        }

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
                MasterKey = this.databaseSettings.AuthorizationKey,
            };

            var leaseCollectionInfo = new DocumentCollectionInfo()
            {
                DatabaseName = DatabaseId,
                CollectionName = leaseCollectionId,
                Uri = new Uri(this.databaseSettings.EndpointUrl),
                MasterKey = this.databaseSettings.AuthorizationKey,
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

        private async Task InitializeAsync((string ServiceId, string Stamp) serviceInfo, bool isProduction)
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
                    OfferThroughput = isProduction ? ServicesRUThroughput : ServicesRUThroughputDev,
                });

            // Ensure we create an entry that backup our leases collection
            await InitializeServiceIdAsync(serviceInfo);

            // Create 'contacts'
            Logger.LogInformation($"Creating Collection:{ContactCollectionId}");
            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(ContactCollectionId),
                new RequestOptions
                {
                    OfferThroughput = isProduction ? ContactsRUThroughput : ContactsRUThroughputDev,
                });

            // Create 'message'
            Logger.LogInformation($"Creating Collection:{MessageCollectionId}");
            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(MessageCollectionId),
                new RequestOptions
                {
                    OfferThroughput = isProduction ? MessageRUThroughput : MessageRUThroughputDev,
                });

            // cleanup all 'stale' leases collections
            var servicesIds = (await GetServiceDocuments(CancellationToken.None)).Select(d => d.Id);
            var staleLeaseCollections = Client.CreateDocumentCollectionQuery(databaseResponse.Resource.SelfLink)
                .ToList()
                .Where(d => d.Id.StartsWith(LeaseCollectionBaseId) && !servicesIds.Contains(d.Id.Substring(LeaseCollectionBaseId.Length)));

            foreach (var docCollection in staleLeaseCollections)
            {
                try
                {
                    Logger.LogInformation($"Delete stale lease collection:{docCollection.Id}");
                    await Client.DeleteDocumentCollectionAsync(docCollection.SelfLink);
                }
                catch (Exception error)
                {
                    Logger.LogError(error, $"Failed to delete stale lease collection:{docCollection.Id}");
                }
            }

            var leaseCollectionId = $"{LeaseCollectionBaseId}{serviceInfo.ServiceId}";

            // Create 'leases'
            Logger.LogInformation($"Creating Collection:{leaseCollectionId}");
            this.providerLeaseColletion = (await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(leaseCollectionId),
                new RequestOptions
                {
                    OfferThroughput = isProduction ? LeasesRUThroughput : LeasesRUThroughputDev,
                })).Resource;

            // Create 'services' processor
            this.feedProcessors.Add(await CreateChangeFeedProcessorAsync(
                ServiceCollectionId,
                leaseCollectionId,
                "PresenceServiceR-Services",
                OnServiceDocumentsChangedAsync));

            // Create 'contacts' processor
            this.feedProcessors.Add(await CreateChangeFeedProcessorAsync(
                ContactCollectionId,
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
