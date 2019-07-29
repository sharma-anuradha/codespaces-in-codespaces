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
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Implements IScaleServiceProvider based on an Azure Cosmos database
    /// </summary>
    public class DatabaseBackplaneProvider : IBackplaneProvider, IAsyncDisposable
    {
        public static readonly string DatabaseId = "presenceService";

        private static readonly string ServiceCollectionId = "services";
        private static readonly string ContactDataCollectionId = "contactsData";
        private static readonly string MessageCollectionId = "messages";
        private static readonly string LeaseCollectionBaseId = "leases-";

        // Logger method scopes
        private const string MethodSendMessage = "DatabaseProvider.SendMessageAsync";
        private const string MethodUpdateContact = "DatabaseProvider.UpdateContact";
        private const string MethodOnServiceDocumentsChanged = "DatabaseProvider.OnServiceDocumentsChanged";
        private const string MethodOnContactDocumentsChanged = "DatabaseProvider.OnContactDocumentsChanged";
        private const string MethodOnMessageDocumentsChanged = "DatabaseProvider.OnMessageDocumentsChanged";

        private const int StaleServiceSeconds = 120;

        // RU values for production
        private const int ServicesRUThroughput = 400;
        private const int ContactsRUThroughput = 5000;
        private const int MessageRUThroughput = 400;
        private const int LeasesRUThroughput = 1000;

        // RU values for Development
        private const int ServicesRUThroughput_Dev = 400;
        private const int ContactsRUThroughput_Dev = 400;
        private const int MessageRUThroughput_Dev = 400;
        private const int LeasesRUThroughput_Dev = 400;

        private readonly DatabaseSettings databaseSettings;
        private readonly List<IChangeFeedProcessor> feedProcessors = new List<IChangeFeedProcessor>();
        private ILogger<DatabaseBackplaneProvider> logger;
        private DocumentCollection providerLeaseColletion;

        private HashSet<string> activeServices;

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
            string serviceId,
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

            await databaseBackplaneProvider.InitializeAsync(serviceId, databaseSettings.IsProduction);
            return databaseBackplaneProvider;
        }

        public DocumentClient Client { get; }

        #region IAsyncDisposable

        public async Task DisposeAsync()
        {
            this.logger.LogDebug($"DatabaseProvider.Dispose");

            foreach (var feedProcessor in this.feedProcessors)
            {
                await feedProcessor.StopAsync();
            }

            await Client.DeleteDocumentCollectionAsync(this.providerLeaseColletion.SelfLink);

            Client.Dispose();
        }

        #endregion

        #region IBackplaneProvider

        public OnContactChangedAsync ContactChangedAsync { get; set; }

        public OnMessageReceivedAsync MessageReceivedAsync { get; set; }

        public int Priority => 0;

        public async Task UpdateMetricsAsync(string serviceId, object serviceInfo, PresenceServiceMetrics metrics, CancellationToken cancellationToken)
        {
            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, ServiceCollectionId);
            await Client.UpsertDocumentAsync(documentCollectionUri, new ServiceDocument()
            {
                Id = serviceId,
                ServiceInfo = serviceInfo,
                Metrics = metrics,
                LastUpdate = DateTime.UtcNow
            }, cancellationToken: cancellationToken);
        }

        public async Task<Dictionary<string, ContactDataInfo>> GetContactsDataAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken)
        {
            var emailPropertyValue = matchProperties.TryGetProperty<string>(Properties.Email)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(emailPropertyValue))
            {
                return new Dictionary<string, ContactDataInfo>();
            }

            var queryable = Client.CreateDocumentQuery<ContactDataDocument>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, ContactDataCollectionId))
                .Where(c => c.Email == emailPropertyValue);

            var matchContacts = await ToListAsync(queryable, cancellationToken);
            return matchContacts.ToDictionary(d => d.Id, d => ToContactData(d));
        }

        public async Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            var contactDataCoument = (await GetContactDataDocumentAsync(contactId, cancellationToken)).Item1;
            return contactDataCoument != null ? ToContactData(contactDataCoument) : null;
        }

        public async Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            using (this.logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, MethodSendMessage)))
            {
                this.logger.LogDebug($"contactId:{messageData.FromContact.Id} targetContactId:{messageData.TargetContact.Id}");
            }

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

        public async Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            ContactDataInfo contactDataInfo;
            var contactDataDocumentInfo = await GetContactDataDocumentAsync(contactDataChanged.ContactId, cancellationToken);
            if (contactDataDocumentInfo.Item1 == null)
            {
                contactDataInfo = new Dictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>();
            }
            else
            {
                contactDataInfo = ToContactData(contactDataDocumentInfo.Item1);
            }

            contactDataInfo.UpdateConnectionProperties(contactDataChanged);

            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, ContactDataCollectionId);
            var resonse = await Client.UpsertDocumentAsync(documentCollectionUri, new ContactDataDocument()
            {
                Id = contactDataChanged.ContactId,
                ServiceId = contactDataChanged.ServiceId,
                ConnectionId = contactDataChanged.ConnectionId,
                UpdateType = contactDataChanged.Type,
                Properties = contactDataChanged.Data.Keys.ToArray(),
                Email = contactDataInfo.GetAggregatedProperties().TryGetProperty<string>(Properties.Email)?.ToLowerInvariant() ?? contactDataDocumentInfo.Item1?.Email,
                ServiceConnections = JObject.FromObject(contactDataInfo),
                LastUpdate = DateTime.UtcNow
            }, cancellationToken: cancellationToken);

            using (this.logger.BeginScope(
                (LoggerScopeHelpers.MethodScope, MethodUpdateContact),
                (LoggerScopeHelpers.MethodPerfScope, (resonse.RequestLatency + contactDataDocumentInfo.Item2).Milliseconds)))
            {
                this.logger.LogDebug($"contactId:{contactDataChanged.ContactId}");
            }
        }

        #endregion

        #region Callback from the Feed processor

        private async Task OnServiceDocumentsChangedAsync(IReadOnlyList<Document> docs)
        {
            using (this.logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, MethodOnServiceDocumentsChanged)))
            {
                this.logger.LogDebug($"servicesIds:{string.Join(",", docs.Select(d => d.Id))}");
            }

            await LoadActiveServicesAsync(default(CancellationToken));
        }

        private async Task OnContactDocumentsChangedAsync(IReadOnlyList<Document> docs)
        {
            var sw = new System.Diagnostics.Stopwatch();

            if (ContactChangedAsync != null)
            {
                foreach (var doc in docs)
                {
                    try
                    {
                        var contact = await ReadAsAsync<ContactDataDocument>(doc);
                        var contactDataInfoChanged = new ContactDataChanged<ContactDataInfo>(
                            contact.ServiceId,
                            contact.ConnectionId,
                            contact.Id,
                            contact.UpdateType,
                            ToContactData(contact));

                        await ContactChangedAsync(contactDataInfoChanged, contact.Properties ?? Array.Empty<string>(), default(CancellationToken));
                    }
                    catch (Exception error)
                    {
                        this.logger.LogError(error, $"Failed when processing contact document:{doc.Id}");

                    }
                }
            }

            using (this.logger.BeginScope(
                (LoggerScopeHelpers.MethodScope, MethodOnContactDocumentsChanged),
                (LoggerScopeHelpers.MethodPerfScope, sw.ElapsedMilliseconds)))
            {
                this.logger.LogDebug($"contactIds:{string.Join(",", docs.Select(d => d.Id))}");
            }

        }

        private async Task OnMessageDocumentsChangedAsync(IReadOnlyList<Document> docs)
        {
            using (this.logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, MethodOnMessageDocumentsChanged)))
            {
                this.logger.LogDebug($"messageIds:{string.Join(",", docs.Select(d => d.Id))}");
            }

            if (MessageReceivedAsync != null)
            {
                foreach (var doc in docs)
                {
                    try
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
                    catch (Exception error)
                    {
                        this.logger.LogError(error, $"Failed when processing message document:{doc.Id}");

                    }
                }
            }
        }

        #endregion

        private async Task<(ContactDataDocument, TimeSpan)> GetContactDataDocumentAsync(string contactId, CancellationToken cancellationToken)
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

        private async Task<List<ServiceDocument>> GetServiceDocuments(CancellationToken cancellationToken)
        {
            var queryable = Client.CreateDocumentQuery<ServiceDocument>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, ServiceCollectionId));

            return await ToListAsync(queryable, cancellationToken);
        }

        private async Task LoadActiveServicesAsync(CancellationToken cancellationToken)
        {
            var allServices = await GetServiceDocuments(cancellationToken);
            var utcNow = DateTime.UtcNow;
            var nonStaleServices = new HashSet<string>(allServices.Where(i => (utcNow - i.LastUpdate).TotalSeconds < StaleServiceSeconds).Select(d => d.Id));

            // next block will delete stale documents
            foreach (var doc in allServices)
            {
                if (!nonStaleServices.Contains(doc.Id))
                {
                    var documentUri = UriFactory.CreateDocumentUri(DatabaseId, ServiceCollectionId, doc.Id);
                    try
                    {
                        await Client.DeleteDocumentAsync(documentUri, cancellationToken: cancellationToken);
                    }
                    catch { }
                }
            }

            // update
            this.activeServices = nonStaleServices;
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
            this.logger.LogInformation($"CreateChangeFeedProcessorAsync collectionId:{collectionId} hostName:{hostName}");

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
                .WithObserverFactory(new CallbackFeedObserverFactory(hostName, onDocumentsChanged, this.logger))
                .BuildAsync();

            await feedProcessor.StartAsync();
            return feedProcessor;
        }

        private async Task InitializeAsync(string serviceId, bool isProduction)
        {
            this.logger.LogInformation($"Creating database:{DatabaseId} if not exists");

            // Create the database
            var databaseResponse = await Client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId });

            // Create 'services'
            this.logger.LogInformation($"Creating Collection:{ServiceCollectionId}");
            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(ServiceCollectionId),
                new RequestOptions
                {
                    OfferThroughput = isProduction ? ServicesRUThroughput : ServicesRUThroughput_Dev
                });

            // Ensure we create an entry that backup our leases collection
            await UpdateMetricsAsync(serviceId, null, default, CancellationToken.None);

            // Create 'contacts'
            this.logger.LogInformation($"Creating Collection:{ContactDataCollectionId}");
            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                CreateDocumentCollectionDefinition(ContactDataCollectionId),
                new RequestOptions
                {
                    OfferThroughput = isProduction ? ContactsRUThroughput : ContactsRUThroughput_Dev
                });

            // Create 'message'
            this.logger.LogInformation($"Creating Collection:{MessageCollectionId}");
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
                    this.logger.LogInformation($"Delete stale lease collection:{docCollection.Id}");
                    await Client.DeleteDocumentCollectionAsync(docCollection.SelfLink);
                }
                catch(Exception error)
                {
                    this.logger.LogError(error, $"Failed to delete stale lease collection:{docCollection.Id}");
                }
            }

            var leaseCollectionId = $"{LeaseCollectionBaseId}{serviceId}";
            // Create 'leases'
            this.logger.LogInformation($"Creating Collection:{leaseCollectionId}");
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

            // load initial services
            await LoadActiveServicesAsync(default);
        }

        private ContactDataInfo ToContactData(ContactDataDocument contactDataDocument)
        {
            var contactDataInfo = ((JObject)contactDataDocument.ServiceConnections).ToObject<ContactDataInfo>();

            // Note: next block will remove 'stale' service entries
            foreach (var serviceId in contactDataInfo.Keys.Where(serviceId => !this.activeServices.Contains(serviceId)).ToArray())
            {
                contactDataInfo.Remove(serviceId);
            }

            return contactDataInfo;
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

        private static object ToObject(JToken jToken)
        {
            return jToken?.Type != JTokenType.Object ? jToken?.ToObject<object>() : jToken;
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
    }
    /// <summary>
    /// Service document model
    /// </summary>
    public class ServiceDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("serviceInfo")]
        public object ServiceInfo { get; set; }

        [JsonProperty("metrics")]
        public PresenceServiceMetrics Metrics { get; set; }

        [JsonProperty("lastUpdate")]
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Contact document model
    /// </summary>
    public class ContactDataDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("serviceId")]
        public string ServiceId { get; set; }

        [JsonProperty("connectionId")]
        public string ConnectionId { get; set; }

        [JsonProperty("properties")]
        public string[] Properties { get; set; }

        [JsonProperty("updateType")]
        public ContactUpdateType UpdateType { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("serviceConnections")]
        public object ServiceConnections { get; set; }

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
