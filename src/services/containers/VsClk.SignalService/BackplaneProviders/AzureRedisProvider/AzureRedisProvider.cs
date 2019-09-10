using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements IScaleServiceProvider based on an Azure redis cache
    /// </summary>
    public class AzureRedisProvider : DocumentDatabaseProvider
    {
        private const string ServicesIndexId = "backplaneProvider:services:index";
        private const string ContactsEmailIndexId = "backplaneProvider:contacts:email";

        private const string ServicesId = "backplaneProvider:services";
        private const string ContactsId = "backplaneProvider:contacts";
        private const string MessagesId = "backplaneProvider:messages";

        private IDatabaseAsync databaseAsync;

        private AzureRedisProvider(
            ConnectionMultiplexer connection,
            ILogger<AzureRedisProvider> logger,
            IFormatProvider formatProvider)
            : base(logger, formatProvider)
        {
            Connection = connection;
        }

        private ConnectionMultiplexer Connection { get; }

        public static async Task<AzureRedisProvider> CreateAsync(
            string serviceId,
            ConnectionMultiplexer connection,
            ILogger<AzureRedisProvider> logger,
            IFormatProvider formatProvider)
        {
            var redisBackplaneProvider = new AzureRedisProvider(connection, logger, formatProvider);
            await redisBackplaneProvider.InitializeAsync(serviceId);
            return redisBackplaneProvider;
        }

        public override int Priority => 10;

        protected override Task DisposeInternalAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task<List<ContactDataDocument>> GetContactsDataByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var allContactsIds = await this.databaseAsync.SetMembersAsync($"{ContactsEmailIndexId}:{email}");
            return await GetDocumentsAsync<ContactDataDocument>(allContactsIds);
        }

        protected override async Task<(ContactDataDocument, TimeSpan)> GetContactDataDocumentAsync(string contactId, CancellationToken cancellationToken)
        {
            var value = await this.databaseAsync.StringGetAsync(ToContactKey(contactId));
            return (DeserializeObject<ContactDataDocument>(value), TimeSpan.FromMilliseconds(0));
        }

        protected override async Task<TimeSpan> UpsertContactDocumentAsync(ContactDataDocument contactDocument, CancellationToken cancellationToken)
        {
            var contactKey = ToContactKey(contactDocument.Id);

            if (!string.IsNullOrEmpty(contactDocument.Email))
            {
                await this.databaseAsync.SetAddAsync($"{ContactsEmailIndexId}:{contactDocument.Email}", contactKey.ToString());
            }

            var json = JsonConvert.SerializeObject(contactDocument);
            await this.databaseAsync.StringSetAsync(contactKey, json);
            await this.databaseAsync.PublishAsync(ContactsId, json);
            return TimeSpan.FromMilliseconds(0);
        }

        protected override async Task InsertMessageDocumentAsync(MessageDocument messageDocument, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(messageDocument);
            await this.databaseAsync.StringSetAsync(ToMessageKey(messageDocument.Id), json);
            await this.databaseAsync.PublishAsync(MessagesId, json);
        }

        protected override async Task UpsertServiceDocumentAsync(ServiceDocument serviceDocument, CancellationToken cancellationToken)
        {
            var key = ToServiceKey(serviceDocument.Id);
            await this.databaseAsync.SetAddAsync(ServicesIndexId, key.ToString());
            var json = JsonConvert.SerializeObject(serviceDocument);
            await this.databaseAsync.StringSetAsync(key, json);
            await this.databaseAsync.PublishAsync(ServicesId, json);
        }

        protected override async Task<List<ServiceDocument>> GetServiceDocuments(CancellationToken cancellationToken)
        {
            var allServicesIds = await this.databaseAsync.SetMembersAsync(ServicesIndexId);
            return await GetDocumentsAsync<ServiceDocument>(allServicesIds);
        }

        protected override async Task DeleteServiceDocumentById(string serviceId, CancellationToken cancellationToken)
        {
            var serviceKey = ToServiceKey(serviceId);
            await this.databaseAsync.SetRemoveAsync(ServicesIndexId, serviceKey.ToString());
            await this.databaseAsync.KeyDeleteAsync(serviceKey);
        }

        private static RedisKey ToContactKey(string contactId) => $"{ContactsId}:{contactId}";
        private static RedisKey ToServiceKey(string serviceId) => $"{ServicesId}:{serviceId}";
        private static RedisKey ToMessageKey(string messageId) => $"{MessagesId}:{messageId}";

        private async Task<List<T>> GetDocumentsAsync<T>(RedisValue[] values)
        {
            return (await this.databaseAsync.StringGetAsync(values
                .Where(v => !v.IsNull)
                .Select(v =>
                {
                    RedisKey key = v.ToString();
                    return key;
                })
                .ToArray()))
                .Select(v => DeserializeObject<T>(v))
                .Where(s => s != null)
                .ToList();
        }

        private async Task InitializeAsync(string serviceId)
        {
            this.databaseAsync = Connection.GetDatabase();
            var subscriber = Connection.GetSubscriber();

            (await subscriber.SubscribeAsync(ServicesId)).OnMessage((message) =>
            {
                return OnServiceDocumentsChangedAsync(new IDocument[] { new Document(message.Message.ToString()) });
            });
            (await subscriber.SubscribeAsync(ContactsId)).OnMessage((message) =>
            {
                return OnContactDocumentsChangedAsync(new IDocument[] { new Document(message.Message.ToString()) });
            });
            (await subscriber.SubscribeAsync(MessagesId)).OnMessage((message) =>
            {
                return OnMessageDocumentsChangedAsync(new IDocument[] { new Document(message.Message.ToString()) });
            });

            // define service Id
            await InitializeServiceIdAsync(serviceId);
        }

        private static T DeserializeObject<T>(RedisValue value)
        {
            return value.IsNull ? default(T) : DeserializeObject<T>(value.ToString());
        }

        private static T DeserializeObject<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonSerializationException)
            {
                return default(T);
            }
        }

        private class Document : IDocument
        {
            private readonly JObject jObject;
            public Document(string json)
            {
                this.jObject = JsonConvert.DeserializeObject<JObject>(json);
            }

            public string Id => this.jObject["id"].ToString();

            public Task<T> ReadAsAsync<T>()
            {
                return Task.FromResult(this.jObject.ToObject<T>());
            }
        }
    }
}
