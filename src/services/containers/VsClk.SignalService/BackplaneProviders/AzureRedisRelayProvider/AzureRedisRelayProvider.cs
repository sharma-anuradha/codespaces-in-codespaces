// <copyright file="AzureRedisRelayProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using RelayHubInfo = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements IRelayBackplaneProvider based on an Azure redis cache
    /// </summary>
    public class AzureRedisRelayProvider : IRelayBackplaneProvider, IRelayBackplaneManagerProvider
    {
        private const string ParticipantChangedId = "relayBackplaneProvider:participant";
        private const string SendDataId = "relayBackplaneProvider:data";
        private const string RelayHubChangedId = "relayBackplaneProvider:relayHub";
        private const string HubsId = "relayBackplaneProvider:hubs";

        private readonly RedisConnectionPool redisConnectionPool;

        private AzureRedisRelayProvider(
            RedisConnectionPool redisConnectionPool,
            ILogger<AzureRedisRelayProvider> logger)
        {
            this.redisConnectionPool = redisConnectionPool;
            Logger = logger;
        }

        public OnRelayDataChangedAsync<RelayParticipantChanged> ParticipantChangedAsync { get; set; }

        public OnRelayDataChangedAsync<SendRelayDataHub> SendDataChangedAsync { get; set; }

        public OnRelayDataChangedAsync<RelayHubChanged> RelayHubChanged { get; set; }

        private ILogger Logger { get; }

        private IDatabaseAsync DatabaseAsync => this.redisConnectionPool.DatabaseAsync;

        public static async Task<AzureRedisRelayProvider> CreateAsync(
            (string ServiceId, string Stamp, string ServiceType) serviceInfo,
            RedisConnectionPool redisConnectionPool,
            ILogger<AzureRedisRelayProvider> logger)
        {
            var redisBackplaneProvider = new AzureRedisRelayProvider(redisConnectionPool, logger);
            await redisBackplaneProvider.InitializeAsync(serviceInfo);
            return redisBackplaneProvider;
        }

        public bool HandleException(string methodName, Exception error)
        {
            // Note: to avoid send to telemetry massive amounts of error we will accept
            // Connection & Timeou exceptions as non critical
            if (error is RedisConnectionException || error is RedisTimeoutException)
            {
                Logger.LogWarning(error, $"Failed to invoke method:{methodName}");
                return true;
            }

            return false;
        }

        public Task UpdateMetricsAsync((string ServiceId, string Stamp, string ServiceType) serviceInfo, RelayServiceMetrics metrics, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DisposeDataChangesAsync(DataChanged[] dataChanges, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task<RelayHubInfo> GetRelayInfoAsync(string hubId, CancellationToken cancellationToken)
        {
            var value = await DatabaseAsync.StringGetAsync(ToHubKey(hubId));
            return DeserializeObject<RelayHubInfo>(value);
        }

        public async Task UpdateRelayHubInfo(string hubId, RelayHubInfo relayHubInfo, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(relayHubInfo);
            await DatabaseAsync.StringSetAsync(ToHubKey(hubId), json);
        }

        public async Task SendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(dataChanged);
            await DatabaseAsync.PublishAsync(SendDataId, json);
        }

        public async Task NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(dataChanged);
            await DatabaseAsync.PublishAsync(ParticipantChangedId, json);
        }

        public async Task NotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(dataChanged);
            await DatabaseAsync.PublishAsync(RelayHubChangedId, json);
            if (dataChanged.ChangeType == RelayHubChangeType.Removed)
            {
                await DatabaseAsync.KeyDeleteAsync(ToHubKey(dataChanged.HubId));
            }
        }

        private static RedisKey ToHubKey(string hubId) => $"{HubsId}:{hubId}";

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

        private async Task InitializeAsync((string ServiceId, string Stamp, string ServiceType) serviceInfo)
        {
            await ConectionSubscribeAsync(this.redisConnectionPool.SubscribeConnection);
        }

        private async Task ConectionSubscribeAsync(ConnectionMultiplexer connection)
        {
            var subscriber = connection.GetSubscriber();

            (await subscriber.SubscribeAsync(SendDataId)).OnMessage((message) =>
            {
                return OnSendDataAsync(message.Message.ToString());
            });
            (await subscriber.SubscribeAsync(ParticipantChangedId)).OnMessage((message) =>
            {
                return OnParticipantChanged(message.Message.ToString());
            });
            (await subscriber.SubscribeAsync(RelayHubChangedId)).OnMessage((message) =>
            {
                return OnRelayHubChanged(message.Message.ToString());
            });
        }

        private async Task OnSendDataAsync(string json)
        {
            if (SendDataChangedAsync != null)
            {
                try
                {
                    var data = DeserializeObject<SendRelayDataHub>(json);
                    await SendDataChangedAsync(data, default);
                }
                catch (Exception error)
                {
                    Logger.LogError(error, $"Failed when processing SendData");
                }
            }
        }

        private async Task OnParticipantChanged(string json)
        {
            if (ParticipantChangedAsync != null)
            {
                try
                {
                    var data = DeserializeObject<RelayParticipantChanged>(json);
                    await ParticipantChangedAsync(data, default);
                }
                catch (Exception error)
                {
                    Logger.LogError(error, $"Failed when processing SendData");
                }
            }
        }

        private async Task OnRelayHubChanged(string json)
        {
            if (RelayHubChanged != null)
            {
                try
                {
                    var data = DeserializeObject<RelayHubChanged>(json);
                    await RelayHubChanged(data, default);
                }
                catch (Exception error)
                {
                    Logger.LogError(error, $"Failed when processing SendData");
                }
            }
        }
    }
}
