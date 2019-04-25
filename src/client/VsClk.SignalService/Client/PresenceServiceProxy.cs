using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Client
{

    /// <summary>
    /// The presence service proxy client that connects to the remote presence Hub
    /// </summary>
    public class PresenceServiceProxy : IPresenceServiceProxy
    {
        private readonly HubConnection connection;

        public PresenceServiceProxy(HubConnection connection, TraceSource trace)
        {
            this.connection = Requires.NotNull(connection, nameof(trace));
            Requires.NotNull(trace, nameof(trace));

            connection.On<string, Dictionary<string, object>>(Methods.UpdateValues, (contactdId, properties) =>
            {
                trace.Verbose($"UpdateProperties-> contactdId:{contactdId} properties:{properties.ConvertToString()}");
                UpdateProperties?.Invoke(this, new UpdatePropertiesEventArgs(contactdId, properties));
            });

            connection.On<string, string, string, JToken>(Methods.ReceiveMessage, (contactdId, fromContactId, messageType,  body) =>
            {
                trace.Verbose($"MessageReceived-> contactdId:{contactdId} fromContactId:{fromContactId} messageType:{messageType} body:{body}");
                MessageReceived?.Invoke(this, new ReceiveMessageEventArgs(contactdId, fromContactId, messageType, body));
            });
        }

        public event EventHandler<UpdatePropertiesEventArgs> UpdateProperties;
        public event EventHandler<ReceiveMessageEventArgs> MessageReceived;

        public Task RegisterSelfContactAsync(string contactId, Dictionary<string, object> initialProperties, CancellationToken cancellationToken)
        {
            return this.connection.InvokeAsync(nameof(IPresenceServiceHub.RegisterSelfContactAsync), contactId, initialProperties, cancellationToken);
        }

        public Task PublishPropertiesAsync(Dictionary<string, object> updateProperties, CancellationToken cancellationToken)
        {
            return this.connection.InvokeAsync(nameof(IPresenceServiceHub.PublishPropertiesAsync), updateProperties, cancellationToken);
        }

        public Task SendMessageAsync(string targetContactId, string messageType, JToken body, CancellationToken cancellationToken)
        {
            return this.connection.InvokeAsync(nameof(IPresenceServiceHub.SendMessageAsync), targetContactId, messageType, body, cancellationToken);
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(string[] targetContactIds, string[] propertyNames, CancellationToken cancellationToken)
        {
            var result = await this.connection.InvokeAsync<JObject>(nameof(IPresenceServiceHub.AddSubcriptionsAsync), targetContactIds, propertyNames, cancellationToken);
            return ToPropertyDictionary(result);
        }
        public async Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(Dictionary<string, object>[] targetContactProperties, string[] propertyNames, bool useStubContact, CancellationToken cancellationToken)
        {
            var jArray = await this.connection.InvokeAsync<JArray>(nameof(IPresenceServiceHub.RequestSubcriptionsAsync), targetContactProperties, propertyNames, useStubContact, cancellationToken);
            return jArray.Select(item =>
            {
                if (item == null || item.Type == JTokenType.Null)
                {
                    return null;
                }

                return ((IDictionary<string, JToken>)item).ToDictionary(kvp => kvp.Key, kvp => ToObject(kvp.Value));
            }).ToArray();
        }

        public Task RemoveSubcriptionPropertiesAsync(string[] targetContactIds, string[] propertyNames, CancellationToken cancellationToken)
        {
            return this.connection.InvokeAsync(nameof(IPresenceServiceHub.RemoveSubcriptionProperties), targetContactIds, propertyNames, cancellationToken);
        }

        public Task RemoveSubscriptionAsync(string[] targetContactIds, CancellationToken cancellationToken)
        {
            return this.connection.InvokeAsync(nameof(IPresenceServiceHub.RemoveSubscription), targetContactIds, cancellationToken);
        }

        public Task UnregisterSelfContactAsync(CancellationToken cancellationToken)
        {
            return this.connection.InvokeAsync(nameof(IPresenceServiceHub.UnregisterSelfContactAsync), cancellationToken);
        }

        public async Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingProperties, CancellationToken cancellationToken)
        {
            var results = await this.connection.InvokeAsync<JArray>(nameof(IPresenceServiceHub.MatchContactsAsync), matchingProperties, cancellationToken);
            return results.Select(item => ToPropertyDictionary((JObject)item)).ToArray();
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> SearchContactsAsync(Dictionary<string, SearchProperty> searchProperties, int? maxCount, CancellationToken cancellationToken)
        {
            var result = await this.connection.InvokeAsync<JObject>(nameof(IPresenceServiceHub.SearchContactsAsync), searchProperties, maxCount, cancellationToken);
            return ToPropertyDictionary(result);
        }

        private static Dictionary<string, Dictionary<string, object>> ToPropertyDictionary(JObject jObject)
        {
            return ((IDictionary<string, JToken>)jObject).ToDictionary(kvp => kvp.Key, kvp => ((IDictionary<string, JToken>)kvp.Value).ToDictionary(kvp2 => kvp2.Key, kvp2 => ToObject(kvp2.Value)));
        }

        private static object ToObject(object value)
        {
            if (value is JToken jToken && jToken.Type != JTokenType.Object)
            {
                return jToken.ToObject<object>();
            }

            return value;
        }

        private static object ToObject(JToken jToken)
        {
            return jToken?.Type != JTokenType.Object ? jToken?.ToObject<object>() : jToken;
        }
    }
}
