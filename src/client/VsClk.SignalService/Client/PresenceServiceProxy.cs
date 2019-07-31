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
    public class PresenceServiceProxy : HubProxyBase, IPresenceServiceProxy
    {
        private const string HubName = "presenceServiceHub";

        public PresenceServiceProxy(HubConnection connection, TraceSource trace, bool useSignalRHub = false)
            : base(connection, useSignalRHub ? HubName : null)
        {
            Requires.NotNull(trace, nameof(trace));

            connection.On<ContactReference, Dictionary<string, object>, string>(ToHubMethodName(Methods.UpdateValues), (contact, properties, targetConnectionId) =>
            {
                trace.Verbose($"UpdateProperties-> contact:{contact} properties:{properties.ConvertToString()}");
                UpdateProperties?.Invoke(this, new UpdatePropertiesEventArgs(contact, properties, targetConnectionId));
            });

            connection.On<ContactReference, ContactReference, string, JToken>(ToHubMethodName(Methods.ReceiveMessage), (targetContact, fromContact, messageType, body) =>
            {
                trace.Verbose($"MessageReceived-> targetContact:{targetContact} fromContact:{fromContact} messageType:{messageType} body:{body}");
                MessageReceived?.Invoke(this, new ReceiveMessageEventArgs(targetContact, fromContact, messageType, body));
            });

            connection.On<ContactReference, ConnectionChangeType>(ToHubMethodName(Methods.ConnectionChanged), (contact, changeType) =>
            {
                trace.Verbose($"ConnectionChanged-> contact:{contact} changeType:{changeType}");
                ConnectionChanged?.Invoke(this, new ConnectionChangedEventArgs(contact, changeType));
            });
        }

        public event EventHandler<UpdatePropertiesEventArgs> UpdateProperties;
        public event EventHandler<ReceiveMessageEventArgs> MessageReceived;
        public event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;

        public async Task<Dictionary<string, Dictionary<string, PropertyValue>>> GetSelfConnectionsAsync(string contactId, CancellationToken cancellationToken)
        {
            var result = await InvokeAsync<JObject>(nameof(IPresenceServiceHub.GetSelfConnectionsAsync), new object[] { contactId }, cancellationToken);
            return ToConnectionsProperties(result);
        }

        public async Task<Dictionary<string, object>> RegisterSelfContactAsync(string contactId, Dictionary<string, object> initialProperties, CancellationToken cancellationToken)
        {
            var registerProperties = await InvokeAsync<Dictionary<string, object>>(nameof(IPresenceServiceHub.RegisterSelfContactAsync), new object[] { contactId, initialProperties }, cancellationToken);
            return registerProperties;
        }

        public Task PublishPropertiesAsync(Dictionary<string, object> updateProperties, CancellationToken cancellationToken)
        {
            return InvokeAsync(nameof(IPresenceServiceHub.PublishPropertiesAsync), new object[] { updateProperties }, cancellationToken);
        }

        public Task SendMessageAsync(ContactReference targetContact, string messageType, object body, CancellationToken cancellationToken)
        {
            return InvokeAsync(nameof(IPresenceServiceHub.SendMessageAsync), new object[] { targetContact, messageType, body }, cancellationToken);
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(ContactReference[] targetContacts, string[] propertyNames, CancellationToken cancellationToken)
        {
            var result = await InvokeAsync<JObject>(nameof(IPresenceServiceHub.AddSubcriptionsAsync), new object[] { targetContacts, propertyNames }, cancellationToken);
            return ToPropertyDictionary(result);
        }
        public async Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(Dictionary<string, object>[] targetContactProperties, string[] propertyNames, bool useStubContact, CancellationToken cancellationToken)
        {
            var jArray = await InvokeAsync<JArray>(nameof(IPresenceServiceHub.RequestSubcriptionsAsync), new object[] { targetContactProperties, propertyNames, useStubContact }, cancellationToken);
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
            return InvokeAsync(nameof(IPresenceServiceHub.RemoveSubscription), new object[] { targetContacts }, cancellationToken);
        }

        public Task UnregisterSelfContactAsync(CancellationToken cancellationToken)
        {
            return InvokeAsync(nameof(IPresenceServiceHub.UnregisterSelfContactAsync), new object[] { }, cancellationToken);
        }

        public async Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingProperties, CancellationToken cancellationToken)
        {
            var results = await InvokeAsync<JArray>(nameof(IPresenceServiceHub.MatchContactsAsync), matchingProperties, cancellationToken);
            return results.Select(item => ToPropertyDictionary((JObject)item)).ToArray();
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> SearchContactsAsync(Dictionary<string, SearchProperty> searchProperties, int? maxCount, CancellationToken cancellationToken)
        {
            var result = await InvokeAsync<JObject>(nameof(IPresenceServiceHub.SearchContactsAsync), new object[] { searchProperties, maxCount }, cancellationToken);
            return ToPropertyDictionary(result);
        }

        private static Dictionary<string, Dictionary<string, object>> ToPropertyDictionary(JObject jObject)
        {
            return ((IDictionary<string, JToken>)jObject).ToDictionary(kvp => kvp.Key, kvp => ((IDictionary<string, JToken>)kvp.Value).ToDictionary(kvp2 => kvp2.Key, kvp2 => ToObject(kvp2.Value)));
        }

        private static Dictionary<string, Dictionary<string, PropertyValue>> ToConnectionsProperties(JObject jObject)
        {
            return ((IDictionary<string, JToken>)jObject).ToDictionary(kvp => kvp.Key, kvp => ((IDictionary<string, JToken>)kvp.Value).ToDictionary(kvp2 => kvp2.Key, kvp2 => kvp2.Value.ToObject<PropertyValue>()));
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
