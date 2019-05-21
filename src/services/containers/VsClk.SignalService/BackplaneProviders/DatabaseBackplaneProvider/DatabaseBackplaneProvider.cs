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
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements IScaleServiceProvider based on an Azure Cosmos database
    /// </summary>
    public class DatabaseBackplaneProvider : IBackplaneProvider, IAsyncDisposable
    {
        public static readonly string DatabaseId = "presenceService";

        private static readonly string ContactCollectionId = "contacts";
        private static readonly string MessageCollectionId = "messages";
        private static readonly string LeaseCollectionId = "leases";

        private const int ContactsRequestPerUnitThroughput = 2500;
        private const int MessageRequestPerUnitThroughput = 2500;
        private const int LeasesRequestPerUnitThroughput = 2500;

        private readonly DatabaseSettings databaseSettings;
        private readonly List<IChangeFeedProcessor> feedProcessors = new List<IChangeFeedProcessor>();
        private ILogger<DatabaseBackplaneProvider> logger;

        private DatabaseBackplaneProvider(
            DatabaseSettings databaseSettings,
            ILogger<DatabaseBackplaneProvider> logger)
        {
            this.databaseSettings = Requires.NotNull(databaseSettings, nameof(databaseSettings));
            Requires.NotNullOrEmpty(databaseSettings.EndpointUrl, nameof(databaseSettings.EndpointUrl));
            Requires.NotNullOrEmpty(databaseSettings.AuthorizationKey, nameof(databaseSettings.AuthorizationKey));
            this.logger = Requires.NotNull(logger, nameof(logger));

            // initialize document client
            Client = new DocumentClient(new Uri(databaseSettings.EndpointUrl), databaseSettings.AuthorizationKey,
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                });
        }

        public static async Task<DatabaseBackplaneProvider> CreateAsync(
            DatabaseSettings databaseSettings,
            ILogger<DatabaseBackplaneProvider> logger,
            bool deleteDabatase = false)
        {
            var databaseBackplaneProvider = new DatabaseBackplaneProvider(databaseSettings, logger);

            if (deleteDabatase)
            {
                try
                {
                    await databaseBackplaneProvider.Client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseBackplaneProvider.DatabaseId));
                }
                catch (DocumentClientException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // the database was not found
                }
            }

            await databaseBackplaneProvider.InitializeAsync();
            return databaseBackplaneProvider;
        }

        public DocumentClient Client { get; }

        #region IAsyncDisposable

        public async Task DisposeAsync()
        {
            foreach (var feedProcessor in this.feedProcessors)
            {
                await feedProcessor.StopAsync();
            }

            Client.Dispose();
        }

        #endregion

        #region IBackplaneProvider

        public OnContactChangedAsync ContactChangedAsync { get; set; }

        public OnMessageReceivedAsync MessageReceivedAsync { get; set; }

        public int Priority => 0;

        public async Task<ContactData[]> GetContactsAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken)
        {
            var emailPropertyValue = matchProperties.TryGetProperty<string>(Properties.Email)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(emailPropertyValue))
            {
                return Array.Empty<ContactData>();
            }

            var queryable = Client.CreateDocumentQuery<ContactDocument>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, ContactCollectionId))
                .Where(c => c.Email == emailPropertyValue);

            var matchContacts = await ToListAsync(queryable);
            return matchContacts.Select(c =>
            {
                var contactData = ToContactData(c);
                contactData.Properties[Properties.IdReserved] = c.Id;
                return contactData;
            }).ToArray();
        }

        public async Task<ContactData> GetContactPropertiesAsync(string contactId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await Client.ReadDocumentAsync<ContactDocument>(
                    UriFactory.CreateDocumentUri(DatabaseId, ContactCollectionId, contactId),
                    cancellationToken: cancellationToken);
                return ToContactData(response.Document);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            this.logger.LogDebug($"DatabaseProvider.SendMessageAsync -> contactId:{messageData.FromContact.Id} targetContactId:{messageData.TargetContact.Id}");

            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, MessageCollectionId);
            await Client.CreateDocumentAsync(documentCollectionUri, new MessageDocument()
            {
                ContactId = messageData.FromContact.Id,
                TargetContactId = messageData.TargetContact.Id,
                TargetConnectionId = messageData.TargetContact.ConnectionId,
                Type = messageData.Type,
                Body = messageData.Body,
                SourceId = sourceId,
                LastUpdate = DateTime.UtcNow
            }, cancellationToken: cancellationToken);
        }

        public async Task UpdateContactAsync(
            string sourceId,
            string connectionId,
            ContactData contactData,
            ContactUpdateType updateContactType,
            CancellationToken cancellationToken)
        {
            this.logger.LogDebug($"DatabaseProvider.UpdateContactAsync -> contactId:{contactData.Id}");

            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, ContactCollectionId);
            await Client.UpsertDocumentAsync(documentCollectionUri, new ContactDocument()
            {
                Id = contactData.Id,
                ConnectionId = connectionId,
                Email = contactData.Properties.TryGetProperty<string>(Properties.Email)?.ToLowerInvariant(),
                Properties = contactData.Properties,
                Connections = contactData.Connections,
                UpdateType = updateContactType,
                SourceId = sourceId,
                LastUpdate = DateTime.UtcNow
            }, cancellationToken: cancellationToken);
        }

        #endregion

        #region Callback from the Feed processor

        private async Task OnContactDocumentsChangedAsync(IReadOnlyList<Document> docs)
        {
            this.logger.LogDebug($"DatabaseProvider.OnContactDocumentsChanged -> contactIds:{string.Join(",", docs.Select(d => d.Id))}");

            if (ContactChangedAsync != null)
            {
                foreach (var doc in docs)
                {
                    var contact = await ReadAsAsync<ContactDocument>(doc);
                    await ContactChangedAsync(
                        contact.SourceId,
                        contact.ConnectionId,
                        ToContactData(contact),
                        contact.UpdateType,
                        default(CancellationToken));
                }
            }
        }

        private async Task OnMessageDocumentsChangedAsync(IReadOnlyList<Document> docs)
        {
            this.logger.LogDebug($"DatabaseProvider.OnMessageDocumentsChangedAsync -> messageIds:{string.Join(",", docs.Select(d => d.Id))}");

            if (MessageReceivedAsync != null)
            {
                foreach (var doc in docs)
                {
                    var message = await ReadAsAsync<MessageDocument>(doc);
                    await MessageReceivedAsync(
                        message.SourceId,
                        new MessageData(
                            new ContactReference(message.ContactId, null),
                            new ContactReference(message.TargetContactId, message.TargetConnectionId),
                            message.Type,
                            JToken.FromObject(message.Body)),
                            default(CancellationToken));
                }
            }
        }

        #endregion

        /// <summary>
        /// Create a change feed processor based on a collection id
        /// </summary>
        /// <param name="collectionId">The target collection id</param>
        /// <param name="hostName">Reference host name</param>
        /// <param name="onDocumentsChanged">Callback</param>
        /// <returns></returns>
        private async Task<IChangeFeedProcessor> CreateChangeFeedProcessorAsync(
            string collectionId,
            string hostName,
            Func<IReadOnlyList<Document>, Task> onDocumentsChanged)
        {
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
                CollectionName = LeaseCollectionId,
                Uri = new Uri(this.databaseSettings.EndpointUrl),
                MasterKey = this.databaseSettings.AuthorizationKey
            };

            var builder = new ChangeFeedProcessorBuilder();
            var feedProcessor = await builder
                .WithHostName(hostName)
                .WithFeedCollection(feedCollectionInfo)
                .WithLeaseCollection(leaseCollectionInfo)
                .WithObserverFactory(new CallbackFeedObserverFactory(hostName, onDocumentsChanged, this.logger))
                .BuildAsync();

            await feedProcessor.StartAsync();
            return feedProcessor;
        }

        private async Task InitializeAsync()
        {
            this.logger.LogInformation($"Creating database:{DatabaseId} if not exists");

            // Create the database
            await Client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId });

            // Create 'contacts'
            this.logger.LogInformation($"Creating Collection:{ContactCollectionId}");
            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(ContactCollectionId),
                new RequestOptions
                {
                    OfferThroughput = ContactsRequestPerUnitThroughput
                });

            // Create 'message'
            this.logger.LogInformation($"Creating Collection:{MessageCollectionId}");
            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(MessageCollectionId),
                new RequestOptions
                {
                    OfferThroughput = MessageRequestPerUnitThroughput
                });

            // Create 'leases'
            this.logger.LogInformation($"Creating Collection:{LeaseCollectionId}");
            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(LeaseCollectionId),
                new RequestOptions
                {
                    OfferThroughput = LeasesRequestPerUnitThroughput
                });

            // Create 'contacts' processor
            this.feedProcessors.Add(await CreateChangeFeedProcessorAsync(
                ContactCollectionId,
                "PresenceServiceR-Contacts",
                OnContactDocumentsChangedAsync));

            // Create 'message' processor
            this.feedProcessors.Add(await CreateChangeFeedProcessorAsync(
                MessageCollectionId,
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

        private static async Task<T> ReadAsAsync<T>(Document d)
        {
            using (var ms = new MemoryStream())
            using (var reader = new StreamReader(ms))
            {
                d.SaveTo(ms);
                ms.Position = 0;
                return JsonConvert.DeserializeObject<T>(await reader.ReadToEndAsync());
            }
        }

        private static Dictionary<string, object> ToProperties(JObject jObject)
        {
            return ((IDictionary<string, JToken>)jObject).ToDictionary(kvp => kvp.Key, kvp => ToObject(kvp.Value));
        }

        private static ContactData ToContactData(ContactDocument contact)
        {
            return new ContactData(
                contact.Id,
                ToProperties((JObject)contact.Properties),
                ((IDictionary<string, JToken>)contact.Connections).ToDictionary(kvp => kvp.Key, kvp => ToProperties((JObject)kvp.Value)));
        }

        private static object ToObject(JToken jToken)
        {
            return jToken?.Type != JTokenType.Object ? jToken?.ToObject<object>() : jToken;
        }

        private static async Task<List<T>> ToListAsync<T>(IDocumentQuery<T> queryable)
        {
            var list = new List<T>();
            while (queryable.HasMoreResults)
            {   //Note that ExecuteNextAsync can return many records in each call
                var response = await queryable.ExecuteNextAsync<T>();
                list.AddRange(response);
            }

            return list;
        }

        private static Task<List<T>> ToListAsync<T>(IQueryable<T> query)
        {
            return ToListAsync(query.AsDocumentQuery());
        }
    }

    /// <summary>
    /// Contact document model
    /// </summary>
    public class ContactDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("connectionId")]
        public string ConnectionId { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("properties")]
        public object Properties { get; set; }

        [JsonProperty("connections")]
        public object Connections { get; set; }

        [JsonProperty("updateType")]
        public ContactUpdateType UpdateType { get; set; }

        [JsonProperty("sourceId")]
        public string SourceId { get; set; }

        [JsonProperty("lastUpdate")]
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Message docuemnt model
    /// </summary>
    public class MessageDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("contactId")]
        public string ContactId { get; set; }

        [JsonProperty("targetContactId")]
        public string TargetContactId { get; set; }

        [JsonProperty("targetConnectionId")]
        public string TargetConnectionId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("body")]
        public object Body { get; set; }

        [JsonProperty("sourceId")]
        public string SourceId { get; set; }

        [JsonProperty("lastUpdate")]
        public DateTime LastUpdate { get; set; }
    }
}
