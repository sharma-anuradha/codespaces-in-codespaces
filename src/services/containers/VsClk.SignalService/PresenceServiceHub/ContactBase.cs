using System;
using System.Collections;
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

        protected ILogger Logger => this.service.Logger;

        /// <summary>
        /// Return all client proxies from a connection id
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        protected IEnumerable<IClientProxy> Clients(string connectionId)
        {
            return this.service.Clients(connectionId);
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

        protected Task NotifyUpdateValuesAsync(
            Tuple<string, string> connectionSubscription,
            Dictionary<string, object> notifyProperties,
            string selfConnectionId,
            CancellationToken cancellationToken)
        {

            using (Logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, PresenceHubMethods.UpdateValues)))
            {
                Logger.LogDebug($"Notify-> connectionSubscription:{connectionSubscription} selfConnectionId:{selfConnectionId} contactId:{ContactId} notifyProperties:{notifyProperties.ConvertToString()}");
            }

            return Task.WhenAll(Clients(connectionSubscription.Item1).Select(client => client.SendAsync(
                PresenceHubMethods.UpdateValues,
                new ContactReference(ContactId, selfConnectionId),
                notifyProperties,
                connectionSubscription.Item2,
                cancellationToken)));
        }

        protected Task NotifyReceiveMessageAsync(
            ContactReference contactReference,
            ContactReference fromContactReference,
            string messageType,
            object body,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginContactReferenceScope(PresenceHubMethods.ReceiveMessage, contactReference))
            {
                Logger.LogDebug($"Notify-> fromContact:{fromContactReference} messageType:{messageType} body:{body}");
            }

            return Task.WhenAll(Clients(contactReference.ConnectionId).Select(client => client.SendAsync(PresenceHubMethods.ReceiveMessage, contactReference, fromContactReference, messageType, body, cancellationToken)));
        }

        protected Task NotifyConnectionChangedAsync(
            string connectionId,
            string selfConnectionId,
            ConnectionChangeType changeType,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, PresenceHubMethods.ConnectionChanged)))
            {
                Logger.LogDebug($"Notify-> connectionId:{connectionId} selfConnectionId:{selfConnectionId} changeType:{changeType}");
            }

            return Task.WhenAll(Clients(connectionId).Select(client => client.SendAsync(
                PresenceHubMethods.ConnectionChanged,
                new ContactReference(ContactId, selfConnectionId),
                changeType,
                cancellationToken)));
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
            Func<string, string, bool> filterSubscription,
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
            Func<string, string, bool> filterSubscription)
        {
            var result = new Dictionary<Tuple<string, string>, Dictionary<string, object>>();
            foreach (var subscription in this.connectionSubscriptions)
            {
                if (filterSubscription?.Invoke(subscription.Key.Item1, subscription.Key.Item2) == false)
                {
                    continue;
                }

                var subscriptionProperties = subscription.Value.Count > 0 && !subscription.Value.Contains("*") ? affectedProperties.Intersect(subscription.Value.Values) : affectedProperties;
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

        protected IEnumerable<Task> GetSendUpdateProperties(
            string connectionId,
            ContactDataProvider contactDataProvider,
            CancellationToken cancellationToken)
        {
            return GetSendUpdateProperties(connectionId, contactDataProvider.Properties.Keys, contactDataProvider, cancellationToken);
        }

        protected IEnumerable<Task> GetSendUpdateProperties(
            string connectionId,
            IEnumerable<string> affectedProperties,
            ContactDataProvider contactDataProvider,
            CancellationToken cancellationToken)
        {
            return GetSendUpdateValues(
                connectionId,
                affectedProperties,
                (selfConnectionId, propertyName) =>
                {
                    object value = null;
                    if (string.IsNullOrEmpty(selfConnectionId))
                    {
                        contactDataProvider.Properties.TryGetValue(propertyName, out value);
                    }
                    else
                    {
                        value = contactDataProvider.GetConnectionPropertyValue(propertyName, selfConnectionId);
                    }

                    return value;
                },
                (notifyConnectionId, selfConnectionId) => selfConnectionId == null || selfConnectionId == connectionId,
                cancellationToken);
        }
    }
}
