using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Contact base class 
    /// </summary>
    internal class ContactBase
    {
        private readonly PresenceService service;

        /// <summary>
        /// Map of connection Id <-> subscriptions
        /// </summary>
        private readonly ConcurrentDictionary<Tuple<string, string>, ConcurrentHashSet<string>> connectionSubscriptions = new ConcurrentDictionary<Tuple<string, string>, ConcurrentHashSet<string>>();

        public ContactBase(PresenceService service, string contactId)
        {
            this.service = Requires.NotNull(service, nameof(service));
            Requires.NotNullOrEmpty(contactId, nameof(contactId));

            ContactId = contactId;
        }

        /// <summary>
        /// The unique contact id for this instance
        /// </summary>
        public string ContactId { get; }

        /// <summary>
        /// If this contact has any subscription
        /// </summary>
        public bool HasSubscriptions => this.connectionSubscriptions.Count > 0;

        /// <summary>
        /// Add a subscription properties to this instance
        /// </summary>
        /// <param name="connectionId">The connection id to track</param>
        /// <param name="selfConnectionId">Optional self connection id</param>
        /// <param name="propertyNames">Properties to track</param>
        public void AddSubcriptionProperties(string connectionId, string selfConnectionId, string[] propertyNames)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNull(propertyNames, nameof(propertyNames));

            this.connectionSubscriptions[new Tuple<string, string>(connectionId, selfConnectionId)] = new ConcurrentHashSet<string>(propertyNames);
        }


        /// <summary>
        /// Remove a subscription being mantained by this contact
        /// </summary>
        /// <param name="connectionId">The tracked connection id</param>
        /// <param name="selfConnectionId">Optional self connection id</param>
        public void RemoveSubscription(string connectionId, string selfConnectionId)
        {
            this.connectionSubscriptions.TryRemove(new Tuple<string, string>(connectionId, selfConnectionId), out var properties);
        }

        /// <summary>
        /// Remove all subscriptions associated with a connection
        /// </summary>
        /// <param name="connectionId"></param>
        public void RemoveAllSubscriptions(string connectionId)
        {
            foreach (var key in this.connectionSubscriptions.Keys.Where(k => k.Item1 == connectionId).ToArray())
            {
                this.connectionSubscriptions.TryRemove(key, out var properties);
            }
        }

        /// <summary>
        /// Notify updated properties for this contact
        /// </summary>
        /// <param name="selfConnectionId">The self connection who caused the update</param>
        /// <param name="contactData">The contact data entity</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendUpdatePropertiesAsync(
            string selfConnectionId,
            ContactData contactData,
            CancellationToken cancellationToken)
        {
            await Task.WhenAll(GetSendUpdateProperties(selfConnectionId, contactData, cancellationToken));
        }

        public Task SendConnectionChangedAsync(
                string connectionId,
                ConnectionChangeType changeType,
                CancellationToken cancellationToken)
        {
            return NotifyConnectionChangedAsync(Array.Empty<string>(), connectionId, changeType, cancellationToken);
        }

        protected ILogger<PresenceService> Logger => this.service.Logger;

        /// <summary>
        /// Return a client proxy from a connection id
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        protected IClientProxy Client(string connectionId)
        {
            return this.service.Hub.Clients.Client(connectionId);
        }

        protected Task NotifyConnectionChangedAsync(
            IEnumerable<string> otherConnections,
            string connectionId,
            ConnectionChangeType changeType,
            CancellationToken cancellationToken)
        {
            return Task.WhenAll(GetSendConnectionChanged(
                GetConnectionSubscriptions().Union(otherConnections),
                connectionId,
                changeType,
                cancellationToken));
        }

        protected IEnumerable<Task> GetSendUpdateProperties(
            string connectionId,
            ContactData contactData,
            CancellationToken cancellationToken)
        {
            return GetSendUpdateValues(
                connectionId,
                contactData.Properties.Keys,
                (selfConnectionId, propertyName) => GetPropertyValue(contactData, propertyName, selfConnectionId),
                (selfConnectionId) => selfConnectionId == null || selfConnectionId == connectionId,
                cancellationToken);
        }

        protected Task NotifyUpdateValuesAsync(
            Tuple<string, string> connectionSubscription,
            Dictionary<string, object> notifyProperties,
            string selfConnectionId,
            CancellationToken cancellationToken)
        {
            var client = Client(connectionSubscription.Item1);
            if (client != null)
            {
                Logger.LogDebug($"Notify->{Methods.UpdateValues} connectionSubscription:{connectionSubscription} selfConnectionId:{selfConnectionId} contactId:{ContactId} notifyProperties:{notifyProperties.ConvertToString()}");
                return client.SendAsync(
                    Methods.UpdateValues,
                    new ContactReference(ContactId, selfConnectionId),
                    notifyProperties,
                    connectionSubscription.Item2,
                    cancellationToken);
            }

            return Task.CompletedTask;
        }

        protected Task NotifyReceiveMessageAsync(
            ContactReference contactReference,
            ContactReference fromContactReference,
            string messageType,
            object body,
            CancellationToken cancellationToken)
        {
            var client = Client(contactReference.ConnectionId);
            if (client != null)
            {
                Logger.LogDebug($"Notify->{Methods.ReceiveMessage} contact:{contactReference} fromContact:{fromContactReference} messageType:{messageType} body:{body}");
                return client.SendAsync(Methods.ReceiveMessage, contactReference, fromContactReference, messageType, body, cancellationToken);
            }

            return Task.CompletedTask;
        }

        protected Task NotifyConnectionChangedAsync(
            string connectionId,
            string selfConnectionId,
            ConnectionChangeType changeType,
            CancellationToken cancellationToken)
        {
            var client = Client(connectionId);
            if (client != null)
            {
                Logger.LogDebug($"Notify->{Methods.ConnectionChanged} selfConnectionId:{selfConnectionId} changeType:{changeType}");
                return client.SendAsync(
                    Methods.ConnectionChanged,
                    new ContactReference(ContactId, selfConnectionId),
                    changeType,
                    cancellationToken);
            }

            return Task.CompletedTask;
        }

        protected IEnumerable<string> GetConnectionSubscriptions()
        {
            return new HashSet<string>(this.connectionSubscriptions.Keys.Select(k => k.Item1));
        }

        protected IEnumerable<Task> GetSendConnectionChanged(
            IEnumerable<string> connections,
            string selfConnectionId,
            ConnectionChangeType changeType,
            CancellationToken cancellationToken)
        {
            return connections.Select(connectionId => NotifyConnectionChangedAsync(connectionId, selfConnectionId, changeType, cancellationToken));
        }

        protected IEnumerable<Task> GetSendUpdateValues(
            string selfConnectionId,
            IEnumerable<string> affectedProperties,
            Func<string, string, object> resolvePropertyValue,
            Func<string, bool> filterSubscription,
            CancellationToken cancellationToken)
        {
            return GetSubscriptionsNotityProperties(
                affectedProperties,
                resolvePropertyValue,
                filterSubscription)
                .Where(kvp => kvp.Value.Count > 0)
                .Select(kvp => NotifyUpdateValuesAsync(kvp.Key, kvp.Value, selfConnectionId, cancellationToken));
        }

        protected Dictionary<Tuple<string, string>, Dictionary<string, object>> GetSubscriptionsNotityProperties(
            IEnumerable<string> affectedProperties,
            Func<string, string, object> resolvePropertyValue,
            Func<string, bool> filterSubscription)
        {
            var result = new Dictionary<Tuple<string, string>, Dictionary<string, object>>();
            foreach (var subscription in this.connectionSubscriptions)
            {
                if (filterSubscription?.Invoke(subscription.Key.Item2) == false)
                {
                    continue;
                }

                var subscriptionProperties = subscription.Value.Count > 0 ? affectedProperties.Intersect(subscription.Value.Values) : affectedProperties;
                if (subscriptionProperties.Any())
                {
                    var notifyProperties = new Dictionary<string, object>();
                    foreach (var propertyName in subscriptionProperties)
                    {
                        notifyProperties[propertyName] = resolvePropertyValue(subscription.Key.Item2, propertyName);
                    }

                    result[subscription.Key] = notifyProperties;
                }
            }

            return result;
        }

        private static object GetPropertyValue(ContactData contactData, string propertyName, string connectionId)
        {
            object value = null;
            if (string.IsNullOrEmpty(connectionId))
            {
                contactData.Properties.TryGetValue(propertyName, out value);
            }
            else if (contactData.Connections.TryGetValue(connectionId, out var properties))
            {
                properties.TryGetValue(propertyName, out value);
            }

            return value;
        }
    }
}
