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
    /// Implements IBackplaneProvider based on an Azure redis cache
    /// </summary>
    public class AzureRedisProvider : DocumentDatabaseProvider
    {
        private const string ServicesIndexId = "backplaneProvider:services:index";
        private const string ContactsEmailIndexId = "backplaneProvider:contacts:email";

        private const string ServicesId = "backplaneProvider:services";
        private const string ContactsId = "backplaneProvider:contacts";
        private const string MessagesId = "backplaneProvider:messages";

        private IDatabaseAsync DatabaseAsync => Connection.GetDatabase();

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
            (string ServiceId, string Stamp) serviceInfo,
            ConnectionMultiplexer connection,
            ILogger<AzureRedisProvider> logger,
            IFormatProvider formatProvider)
        {
            var redisBackplaneProvider = new AzureRedisProvider(connection, logger, formatProvider);
            await redisBackplaneProvider.InitializeAsync(serviceInfo);
            return redisBackplaneProvider;
        }

        public override int Priority => 10;

        public override bool HandleException(string methodName, Exception error)
        {
            // Note: to avoid send to telemetry massive amounts of error we will accept
            // Connection & Timeou exceptions as non critical
            if (error is RedisConnectionException || error is RedisTimeoutException)
            {
                Logger.LogWarning(error, $"Failed to invoke method:{methodName}");
                return true;
            }

            return base.HandleException(methodName, error);
        }

        protected override Task DisposeInternalAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task<List<ContactDocument>> GetContactsDataByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var allContactsIds = await DatabaseAsync.SetMembersAsync($"{ContactsEmailIndexId}:{email}");
            return await GetDocumentsAsync<ContactDocument>(allContactsIds);
        }

        protected override async Task<ContactDocument> GetContactDataDocumentAsync(string contactId, CancellationToken cancellationToken)
        {
            var value = await DatabaseAsync.StringGetAsync(ToContactKey(contactId));
            return DeserializeObject<ContactDocument>(value);
        }

        protected override async Task UpsertContactDocumentAsync(ContactDocument contactDocument, CancellationToken cancellationToken)
        {
            var contactKey = ToContactKey(contactDocument.Id);

            if (!string.IsNullOrEmpty(contactDocument.Email))
            {
                await DatabaseAsync.SetAddAsync($"{ContactsEmailIndexId}:{contactDocument.Email}", contactKey.ToString());
            }

            var json = JsonConvert.SerializeObject(contactDocument);
            await DatabaseAsync.StringSetAsync(contactKey, json);
            await DatabaseAsync.PublishAsync(ContactsId, json);
        }

        protected override async Task InsertMessageDocumentAsync(MessageDocument messageDocument, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(messageDocument);
            await DatabaseAsync.StringSetAsync(ToMessageKey(messageDocument.Id), json);
            await DatabaseAsync.PublishAsync(MessagesId, json);
        }

        protected override async Task UpsertServiceDocumentAsync(ServiceDocument serviceDocument, CancellationToken cancellationToken)
        {
            var key = ToServiceKey(serviceDocument.Id);
            await DatabaseAsync.SetAddAsync(ServicesIndexId, key.ToString());
            var json = JsonConvert.SerializeObject(serviceDocument);
            await DatabaseAsync.StringSetAsync(key, json);
            await DatabaseAsync.PublishAsync(ServicesId, json);
        }

        protected override async Task<List<ServiceDocument>> GetServiceDocuments(CancellationToken cancellationToken)
        {
            var allServicesIds = await DatabaseAsync.SetMembersAsync(ServicesIndexId);
            return await GetDocumentsAsync<ServiceDocument>(allServicesIds);
        }

        protected override async Task DeleteServiceDocumentById(string serviceId, CancellationToken cancellationToken)
        {
            var serviceKey = ToServiceKey(serviceId);
            await DatabaseAsync.SetRemoveAsync(ServicesIndexId, serviceKey.ToString());
            await DatabaseAsync.KeyDeleteAsync(serviceKey);
        }

        protected override async Task DeleteMessageDocumentByIds(string[] changeIds, CancellationToken cancellationToken)
        {
            var messageKeys = changeIds.Select(changeId => ToMessageKey(changeId)).ToArray();
            await DatabaseAsync.KeyDeleteAsync(messageKeys);
        }

        private static RedisKey ToContactKey(string contactId) => $"{ContactsId}:{contactId}";
        private static RedisKey ToServiceKey(string serviceId) => $"{ServicesId}:{serviceId}";
        private static RedisKey ToMessageKey(string messageId) => $"{MessagesId}:{messageId}";

        private async Task<List<T>> GetDocumentsAsync<T>(RedisValue[] values)
        {
            return (await DatabaseAsync.StringGetAsync(values
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

        private async Task InitializeAsync((string ServiceId, string Stamp) serviceInfo)
        {
            // define service Id
            await InitializeServiceIdAsync(serviceInfo);

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
