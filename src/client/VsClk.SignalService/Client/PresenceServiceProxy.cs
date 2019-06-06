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

            connection.On<ContactReference, Dictionary<string, object>, string>(Methods.UpdateValues, (contact, properties, targetConnectionId) =>
            {
                trace.Verbose($"UpdateProperties-> contact:{contact} properties:{properties.ConvertToString()}");
                UpdateProperties?.Invoke(this, new UpdatePropertiesEventArgs(contact, properties, targetConnectionId));
            });

            connection.On<ContactReference, ContactReference, string, JToken>(Methods.ReceiveMessage, (targetContact, fromContact, messageType, body) =>
            {
                trace.Verbose($"MessageReceived-> targetContact:{targetContact} fromContact:{fromContact} messageType:{messageType} body:{body}");
                MessageReceived?.Invoke(this, new ReceiveMessageEventArgs(targetContact, fromContact, messageType, body));
            });

            connection.On<ContactReference, ConnectionChangeType>(Methods.ConnectionChanged, (contact, changeType) =>
            {
                trace.Verbose($"ConnectionChanged-> contact:{contact} changeType:{changeType}");
                ConnectionChanged?.Invoke(this, new ConnectionChangedEventArgs(contact, changeType));
            });
        }

        public event EventHandler<UpdatePropertiesEventArgs> UpdateProperties;
        public event EventHandler<ReceiveMessageEventArgs> MessageReceived;
        public event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;

        public async Task<Dictionary<string, Dictionary<string, object>>> GetSelfConnectionsAsync(string contactId, CancellationToken cancellationToken)
        {
            var result = await this.connection.InvokeAsync<JObject>(nameof(IPresenceServiceHub.GetSelfConnectionsAsync), contactId, cancellationToken);
            return ToPropertyDictionary(result);
        }

        public async Task<Dictionary<string, object>> RegisterSelfContactAsync(string contactId, Dictionary<string, object> initialProperties, CancellationToken cancellationToken)
        {
            var registerProperties = await this.connection.InvokeAsync<Dictionary<string, object>>(nameof(IPresenceServiceHub.RegisterSelfContactAsync), contactId, initialProperties, cancellationToken);
            return registerProperties;
        }

        public Task PublishPropertiesAsync(Dictionary<string, object> updateProperties, CancellationToken cancellationToken)
        {
            return this.connection.InvokeAsync(nameof(IPresenceServiceHub.PublishPropertiesAsync), updateProperties, cancellationToken);
        }

        public Task SendMessageAsync(ContactReference targetContact, string messageType, object body, CancellationToken cancellationToken)
        {
            return this.connection.InvokeAsync(nameof(IPresenceServiceHub.SendMessageAsync), targetContact, messageType, body, cancellationToken);
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(ContactReference[] targetContacts, string[] propertyNames, CancellationToken cancellationToken)
        {
            var result = await this.connection.InvokeAsync<JObject>(nameof(IPresenceServiceHub.AddSubcriptionsAsync), targetContacts, propertyNames, cancellationToken);
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

        public Task RemoveSubscriptionAsync(ContactReference[] targetContacts, CancellationToken cancellationToken)
        {
            return this.connection.InvokeAsync(nameof(IPresenceServiceHub.RemoveSubscription), targetContacts, cancellationToken);
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
