// <copyright file="AzureDocumentsProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements IBackplaneProvider based on an Azure Cosmos database.
    /// </summary>
    public class AzureDocumentsProvider : DocumentDatabaseProvider, IContactBackplaneProvider
    {
        public static readonly string DatabaseId = "presenceService";

        private const string DefaultPartitionKeyPath = "/_partitionKey";

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
        private readonly List<ChangeFeedProcessor> changeFeedProcessors = new List<ChangeFeedProcessor>();

        private Container servicesContainer;
        private Container contactsContainer;
        private Container messagesContainer;
        private Container providerLeaseContainer;

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
            Client = new CosmosClient(
                databaseSettings.EndpointUrl,
                databaseSettings.AuthorizationKey);
        }

        public CosmosClient Client { get; }

        public static async Task<AzureDocumentsProvider> CreateAsync(
            (string ServiceId, string Stamp, string ServiceType) serviceInfo,
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
                    await databaseBackplaneProvider.Client.GetDatabase(DatabaseId).DeleteAsync();
                }
                catch (CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // the database was not found
                }
            }

            await databaseBackplaneProvider.InitializeAsync(serviceInfo, databaseSettings.IsProduction);
            return databaseBackplaneProvider;
        }

        protected override async Task DisposeInternalAsync()
        {
            foreach (var feedProcessor in this.changeFeedProcessors)
            {
                await feedProcessor.StopAsync();
            }

            await this.providerLeaseContainer.DeleteContainerAsync();
            Client.Dispose();
        }

        protected override async Task<List<ContactDocument>[]> GetContactsDataByEmailAsync(string[] emails, CancellationToken cancellationToken)
        {
            var results = Enumerable.Repeat(0, emails.Length).Select(i => new List<ContactDocument>()).ToArray();

            var queryParameters = new List<(string, string)>();
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
                queryParameters.Add((paramName, email));
                ++next;
            }

            var queryDefinition = new QueryDefinition($"SELECT * FROM c where {whereCondition.ToString()}");
            queryParameters.ForEach(i => queryDefinition.WithParameter(i.Item1, i.Item2));

            var allMatchingContacts = await ExecuteWithRetries(() =>
            {
                var feedIterator = this.contactsContainer.GetItemQueryIterator<ContactDocument>(queryDefinition);
                return ToListAsync(feedIterator, cancellationToken);
            });

            foreach (var item in allMatchingContacts)
            {
                var emailBucket = results[emailIndexMap[item.Email]];
                emailBucket.Add(item);
            }

            return results;
        }

        protected override async Task UpsertServiceDocumentAsync(ServiceDocument serviceDocument, CancellationToken cancellationToken)
        {
            var response = await ExecuteWithRetries(() => this.servicesContainer.UpsertItemAsync(serviceDocument, cancellationToken: cancellationToken));
        }

        protected override async Task InsertMessageDocumentAsync(MessageDocument messageDocument, CancellationToken cancellationToken)
        {
            var response = await ExecuteWithRetries(() => this.messagesContainer.CreateItemAsync(messageDocument, cancellationToken: cancellationToken));
        }

        protected override async Task UpsertContactDocumentAsync(ContactDocument contactDocument, CancellationToken cancellationToken)
        {
            var response = await ExecuteWithRetries(() => this.contactsContainer.UpsertItemAsync(contactDocument, cancellationToken: cancellationToken));
        }

        protected override async Task<ContactDocument> GetContactDataDocumentAsync(string contactId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await ExecuteWithRetries(() => this.contactsContainer.ReadItemAsync<ContactDocument>(id: contactId, PartitionKey.None, cancellationToken: cancellationToken));
                return response.Resource;
            }
            catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        protected override async Task<List<ServiceDocument>> GetServiceDocuments(CancellationToken cancellationToken)
        {
            return await ToListAsync(this.servicesContainer.GetItemQueryIterator<ServiceDocument>(), cancellationToken);
        }

        protected override async Task DeleteServiceDocumentById(string serviceId, CancellationToken cancellationToken)
        {
            var response = await this.servicesContainer.DeleteItemAsync<ServiceDocument>(id: serviceId, PartitionKey.None, cancellationToken: cancellationToken);
        }

        protected override async Task DeleteMessageDocumentByIds(string[] changeIds, CancellationToken cancellationToken)
        {
            foreach (var changeId in changeIds)
            {
                try
                {
                    var response = await ExecuteWithRetries(() => this.messagesContainer.DeleteItemAsync<MessageDocument>(changeId, PartitionKey.None, cancellationToken: cancellationToken));
                }
                catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
                {
                    // the record was not found
                }
            }
        }

        private static async Task<List<T>> ToListAsync<T>(FeedIterator<T> feedIterator, CancellationToken token)
        {
            var list = new List<T>();
            while (feedIterator.HasMoreResults)
            {
                // Note that ReadNextAsync can return many records in each call
                var response = await feedIterator.ReadNextAsync(token);
                list.AddRange(response);
            }

            return list;
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
                catch (CosmosException de)
                {
                    if ((int)de.StatusCode != StatusCodes.Status429TooManyRequests)
                    {
                        throw;
                    }

                    sleepTime = de.RetryAfter.HasValue ? de.RetryAfter.Value : TimeSpan.FromMilliseconds(100);
                }
                catch (AggregateException ae)
                {
                    if (!(ae.InnerException is CosmosException ||
                        ae.InnerExceptions.Any(e => e is CosmosException)))
                    {
                        throw;
                    }

                    CosmosException de = (CosmosException)ae.InnerException;
                    if ((int)de.StatusCode != StatusCodes.Status429TooManyRequests)
                    {
                        throw;
                    }

                    sleepTime = de.RetryAfter.HasValue ? de.RetryAfter.Value : TimeSpan.FromMilliseconds(100);
                }

                await Task.Delay(sleepTime);
            }
        }

        private async Task CreateChangeFeedProcessorAsync<TDocument>(
            Container container,
            Container leaseContainer,
            string processorName,
            string instanceName,
            Func<IReadOnlyCollection<TDocument>, CancellationToken, Task> onDocumentsChanged)
        {
            Logger.LogInformation($"CreateChangeFeedProcessorAsync collectionId:{container.Id} processorName:{processorName}");

            var changeFeedProcessor = container
                .GetChangeFeedProcessorBuilder<TDocument>(processorName, (changes, ct) => onDocumentsChanged(changes, ct))
                    .WithLeaseContainer(leaseContainer)
                    .WithInstanceName(instanceName)
                    .Build();

            await changeFeedProcessor.StartAsync();
            this.changeFeedProcessors.Add(changeFeedProcessor);
        }

        private async Task InitializeAsync((string ServiceId, string Stamp, string ServiceType) serviceInfo, bool isProduction, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation($"Creating database:{DatabaseId} if not exists");

            // Create the database
            Database database = await Client.CreateDatabaseIfNotExistsAsync(DatabaseId);

            // Create 'services'
            Logger.LogInformation($"Creating Collection:{ServiceCollectionId}");
            this.servicesContainer = await database.CreateContainerIfNotExistsAsync(
                ServiceCollectionId,
                DefaultPartitionKeyPath,
                isProduction ? ServicesRUThroughput : ServicesRUThroughputDev);

            // Ensure we create an entry that backup our leases collection
            await InitializeServiceIdAsync(serviceInfo);

            // Create 'contacts'
            Logger.LogInformation($"Creating Collection:{ContactCollectionId}");
            this.contactsContainer = await database.CreateContainerIfNotExistsAsync(
                ContactCollectionId,
                DefaultPartitionKeyPath,
                isProduction ? ContactsRUThroughput : ContactsRUThroughputDev);

            // Create 'message'
            Logger.LogInformation($"Creating Collection:{MessageCollectionId}");
            this.messagesContainer = await database.CreateContainerIfNotExistsAsync(
                MessageCollectionId,
                DefaultPartitionKeyPath,
                isProduction ? MessageRUThroughput : MessageRUThroughputDev);

            // cleanup all 'stale' leases collections
            var servicesIds = (await GetServiceDocuments(CancellationToken.None)).Select(d => d.Id);
            var allContainers = await ToListAsync(database.GetContainerQueryIterator<ContainerProperties>(), cancellationToken);
            var staleLeaseContainers = allContainers
                .Where(props => props.Id.StartsWith(LeaseCollectionBaseId) && !servicesIds.Contains(props.Id.Substring(LeaseCollectionBaseId.Length)));

            foreach (var staleLeaseContainer in staleLeaseContainers)
            {
                try
                {
                    Logger.LogInformation($"Delete stale lease collection:{staleLeaseContainer.Id}");
                    await database.GetContainer(staleLeaseContainer.Id).DeleteContainerAsync();
                }
                catch (Exception error)
                {
                    Logger.LogError(error, $"Failed to delete stale lease collection:{staleLeaseContainer.Id}");
                }
            }

            var leaseContainerId = $"{LeaseCollectionBaseId}{serviceInfo.ServiceId}";

            // Create 'leases'
            Logger.LogInformation($"Creating Collection:{leaseContainerId}");
            this.providerLeaseContainer = await database.CreateContainerIfNotExistsAsync(
                leaseContainerId,
                "/id",
                isProduction ? LeasesRUThroughput : LeasesRUThroughputDev);

            var instanceName = $"{serviceInfo.ServiceId}-instance";

            // Create 'services' processor
            await CreateChangeFeedProcessorAsync<ServiceDocument>(
                this.servicesContainer,
                this.providerLeaseContainer,
                "PresenceServiceR-Services",
                instanceName,
                OnServiceDocumentsChangedAsync);

            // Create 'contacts' processor
            await CreateChangeFeedProcessorAsync<ContactDocument>(
                this.contactsContainer,
                this.providerLeaseContainer,
                "PresenceServiceR-Contacts",
                instanceName,
                OnContactDocumentsChangedAsync);

            // Create 'message' processor
            await CreateChangeFeedProcessorAsync<MessageDocument>(
                this.messagesContainer,
                this.providerLeaseContainer,
                "PresenceServiceR-Messages",
                instanceName,
                OnMessageDocumentsChangedAsync);
        }
    }
}
