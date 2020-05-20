// <copyright file="AzureRedisContactsProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements IBackplaneProvider based on an Azure redis cache.
    /// </summary>
    public class AzureRedisContactsProvider : DocumentDatabaseProvider
    {
        private const string ServicesIndexId = "backplaneProvider:services:index";
        private const string ContactsEmailIndexId = "backplaneProvider:contacts:email";

        private const string ServicesId = "backplaneProvider:services";
        private const string ContactsId = "backplaneProvider:contacts";
        private const string MessagesId = "backplaneProvider:messages";

        private const int ContactsDrainThreshold = 500;

        private readonly RedisConnectionPool redisConnectionPool;
        private ChannelMessageQueue serviceDocumentsChannel;
        private ChannelMessageQueue contactDocumentsChannel;
        private ChannelMessageQueue messageDocumentsChannel;

        private AzureRedisContactsProvider(
            RedisConnectionPool redisConnectionPool,
            ILogger<AzureRedisContactsProvider> logger,
            IServiceCounters serviceCounters,
            IFormatProvider formatProvider)
            : base(logger, serviceCounters, formatProvider)
        {
            this.redisConnectionPool = redisConnectionPool;
        }

        private IDatabaseAsync DatabaseAsync => this.redisConnectionPool.DatabaseAsync;

        public static async Task<AzureRedisContactsProvider> CreateAsync(
            ServiceInfo serviceInfo,
            RedisConnectionPool redisConnectionPool,
            ILogger<AzureRedisContactsProvider> logger,
            IServiceCounters serviceCounters,
            IFormatProvider formatProvider)
        {
            var redisBackplaneProvider = new AzureRedisContactsProvider(redisConnectionPool, logger, serviceCounters, formatProvider);
            await redisBackplaneProvider.InitializeAsync(serviceInfo);
            return redisBackplaneProvider;
        }

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

        protected override async Task<List<ContactDocument>[]> GetContactsDataByEmailAsync(string[] emails, CancellationToken cancellationToken)
        {
            var results = new List<ContactDocument>[emails.Length];

            Func<int, string, Task> emailDocumentTask = async (index, email) =>
            {
                var allContactsIds = await DatabaseAsync.SetMembersAsync($"{ContactsEmailIndexId}:{email}");
                results[index] = await GetDocumentsAsync<ContactDocument>(allContactsIds);
            };

            int next = 0;
            await Task.WhenAll(emails.Select(email => emailDocumentTask(next++, email)).ToArray());
            return results;
        }

        protected override async Task<ContactDocument> GetContactDataDocumentAsync(string contactId, CancellationToken cancellationToken)
        {
            var value = await DatabaseAsync.StringGetAsync(ToContactKey(contactId));
            return DeserializeObject<ContactDocument>(value);
        }

        protected override async Task UpsertContactDocumentAsync(ContactDocument contactDocument, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            var contactKey = ToContactKey(contactDocument.Id);

            if (!string.IsNullOrEmpty(contactDocument.Email))
            {
                tasks.Add(DatabaseAsync.SetAddAsync($"{ContactsEmailIndexId}:{contactDocument.Email}", contactKey.ToString()));
            }

            var json = JsonConvert.SerializeObject(contactDocument);
            tasks.Add(DatabaseAsync.StringSetAsync(contactKey, json));
            tasks.Add(DatabaseAsync.PublishAsync(ContactsId, json));
            tasks.Add(DrainChannelMessageQueue<ContactDocument>(this.contactDocumentsChannel, OnContactDocumentsChangedAsync, ContactsDrainThreshold));
            await Task.WhenAll(tasks);
        }

        protected override async Task InsertMessageDocumentAsync(MessageDocument messageDocument, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(messageDocument);
            var tasks = new List<Task>();
            tasks.Add(DatabaseAsync.StringSetAsync(ToMessageKey(messageDocument.Id), json));
            tasks.Add(DatabaseAsync.PublishAsync(MessagesId, json));
            await Task.WhenAll(tasks);
        }

        protected override async Task UpsertServiceDocumentAsync(ServiceDocument serviceDocument, CancellationToken cancellationToken)
        {
            var key = ToServiceKey(serviceDocument.Id);
            var tasks = new List<Task>();
            tasks.Add(DatabaseAsync.SetAddAsync(ServicesIndexId, key.ToString()));
            var json = JsonConvert.SerializeObject(serviceDocument);
            tasks.Add(DatabaseAsync.StringSetAsync(key, json));
            tasks.Add(DatabaseAsync.PublishAsync(ServicesId, json));
            await Task.WhenAll(tasks);
        }

        protected override async Task<List<ServiceDocument>> GetServiceDocuments(CancellationToken cancellationToken)
        {
            var allServicesIds = await DatabaseAsync.SetMembersAsync(ServicesIndexId);
            return await GetDocumentsAsync<ServiceDocument>(allServicesIds);
        }

        protected override async Task DeleteServiceDocumentById(string serviceId, CancellationToken cancellationToken)
        {
            var serviceKey = ToServiceKey(serviceId);
            var tasks = new List<Task>();
            tasks.Add(DatabaseAsync.SetRemoveAsync(ServicesIndexId, serviceKey.ToString()));
            tasks.Add(DatabaseAsync.KeyDeleteAsync(serviceKey));
            await Task.WhenAll(tasks);
        }

        protected override async Task DeleteMessageDocumentByIds(string[] changeIds, CancellationToken cancellationToken)
        {
            var messageKeys = changeIds.Select(changeId => ToMessageKey(changeId)).ToArray();
            await DatabaseAsync.KeyDeleteAsync(messageKeys);
        }

        private static RedisKey ToContactKey(string contactId) => $"{ContactsId}:{contactId}";

        private static RedisKey ToServiceKey(string serviceId) => $"{ServicesId}:{serviceId}";

        private static RedisKey ToMessageKey(string messageId) => $"{MessagesId}:{messageId}";

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

        private static async Task<ChannelMessageQueue> InitializeChannelMessageQueueAsync<T>(
            ISubscriber subscriber,
            RedisChannel redisChannel,
            Func<IReadOnlyCollection<T>, CancellationToken, Task> onMessageCallback)
        {
            var channelMessageQueue = await subscriber.SubscribeAsync(redisChannel);
            channelMessageQueue.OnMessage(message => onMessageCallback(new T[] { JsonConvert.DeserializeObject<T>(message.Message.ToString()) }, default));
            return channelMessageQueue;
        }

        private async Task DrainChannelMessageQueue<T>(
            ChannelMessageQueue channelMessageQueue,
            Func<IReadOnlyCollection<T>, CancellationToken, Task> onMessageCallback,
            int threshold)
        {
            if (channelMessageQueue.TryGetCount(out var count) && count > threshold)
            {
                Logger.LogDebug($"Drain channel:{channelMessageQueue.Channel} count:{count}");

                ChannelMessage item;
                var docs = new T[1];
                while (channelMessageQueue.TryRead(out item))
                {
                    docs[0] = JsonConvert.DeserializeObject<T>(item.Message.ToString());
                    await onMessageCallback(docs, default);
                }
            }
        }

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

        private async Task InitializeAsync(ServiceInfo serviceInfo)
        {
            // define service Id
            await InitializeServiceIdAsync(serviceInfo);

            await ConectionSubscribeAsync(this.redisConnectionPool.SubscribeConnection);
        }

        private async Task ConectionSubscribeAsync(ConnectionMultiplexer connection)
        {
            var subscriber = connection.GetSubscriber();
            this.serviceDocumentsChannel = await InitializeChannelMessageQueueAsync<ServiceDocument>(subscriber, ServicesId, OnServiceDocumentsChangedAsync);
            this.contactDocumentsChannel = await InitializeChannelMessageQueueAsync<ContactDocument>(subscriber, ContactsId, OnContactDocumentsChangedAsync);
            this.messageDocumentsChannel = await InitializeChannelMessageQueueAsync<MessageDocument>(subscriber, MessagesId, OnMessageDocumentsChangedAsync);
        }
    }
}
