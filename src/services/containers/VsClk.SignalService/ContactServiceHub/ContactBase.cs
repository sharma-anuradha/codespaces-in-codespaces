// <copyright file="ContactBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
    /// Contact base class.
    /// </summary>
    internal class ContactBase
    {
        /// <summary>
        /// Map of connection Id with property name subscriptions.
        /// </summary>
        private readonly Dictionary<Tuple<string, string>, string[]> connectionSubscriptions = new Dictionary<Tuple<string, string>, string[]>();

        private readonly object connectionSubscriptionsLock = new object();

        public ContactBase(ContactService service, string contactId)
        {
            Service = Requires.NotNull(service, nameof(service));
            Requires.NotNullOrEmpty(contactId, nameof(contactId));

            ContactId = contactId;
        }

        /// <summary>
        /// gets the unique contact id for this instance.
        /// </summary>
        public string ContactId { get; }

        /// <summary>
        /// Gets a value indicating whether if this contact has any subscription.
        /// </summary>
        public bool HasSubscriptions => this.connectionSubscriptions.Count > 0;

        protected ContactService Service { get; }

        protected ILogger Logger => Service.Logger;

        /// <summary>
        /// Add a subscription properties to this instance.
        /// </summary>
        /// <param name="connectionId">The connection id to track.</param>
        /// <param name="selfConnectionId">Optional self connection id.</param>
        /// <param name="propertyNames">Properties to track.</param>
        public void AddSubcriptionProperties(string connectionId, string selfConnectionId, string[] propertyNames)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNull(propertyNames, nameof(propertyNames));

            lock (this.connectionSubscriptionsLock)
            {
                this.connectionSubscriptions[ToKey(connectionId, selfConnectionId)] = new HashSet<string>(propertyNames).ToArray();
            }
        }

        /// <summary>
        /// Remove a subscription being mantained by this contact.
        /// </summary>
        /// <param name="connectionId">The tracked connection id.</param>
        /// <param name="selfConnectionId">Optional self connection id.</param>
        /// <returns>If the item was really removed.</returns>
        public bool RemoveSubscription(string connectionId, string selfConnectionId)
        {
            lock (this.connectionSubscriptionsLock)
            {
                return this.connectionSubscriptions.Remove(ToKey(connectionId, selfConnectionId));
            }
        }

        /// <summary>
        /// Remove all subscriptions associated with a connection.
        /// </summary>
        /// <param name="connectionId">The connection id.</param>
        public void RemoveAllSubscriptions(string connectionId)
        {
            lock (this.connectionSubscriptionsLock)
            {
                foreach (var key in this.connectionSubscriptions.Keys.Where(k => k.Item1 == connectionId).ToArray())
                {
                    this.connectionSubscriptions.Remove(key);
                }
            }
        }

        /// <summary>
        /// Return all client proxies from a connection id.
        /// </summary>
        /// <param name="connectionId">The connection id.</param>
        /// <returns>Enumerable of IClientProxy.</returns>
        protected IEnumerable<IClientProxy> Clients(string connectionId)
        {
            return Service.Clients(connectionId);
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
                (LoggerScopeHelpers.MethodScope, ContactHubMethods.UpdateValues)))
            {
                Logger.LogDebug($"Notify-> connectionSubscription:{connectionSubscription} selfConnectionId:{selfConnectionId} contactId:{Service.FormatContactId(ContactId)} notifyProperties:{notifyProperties.ConvertToString(Service.FormatProvider)}");
            }

            return Task.WhenAll(Clients(connectionSubscription.Item1).Select(client => client.SendAsync(
                ContactHubMethods.UpdateValues,
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
            using (Logger.BeginContactReferenceScope(ContactHubMethods.ReceiveMessage, contactReference, Service.FormatProvider))
            {
                Logger.LogDebug($"Notify-> fromContact:{fromContactReference.ToString(Service.FormatProvider)} messageType:{messageType} body:{Service.Format("{0:K}", body?.ToString())}");
            }

            return Task.WhenAll(Clients(contactReference.ConnectionId).Select(client => client.SendAsync(ContactHubMethods.ReceiveMessage, contactReference, fromContactReference, messageType, body, cancellationToken)));
        }

        protected Task NotifyConnectionChangedAsync(
            string connectionId,
            string selfConnectionId,
            ConnectionChangeType changeType,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, ContactHubMethods.ConnectionChanged)))
            {
                Logger.LogDebug($"Notify-> connectionId:{connectionId} selfConnectionId:{selfConnectionId} changeType:{changeType}");
            }

            return Task.WhenAll(Clients(connectionId).Select(client => client.SendAsync(
                ContactHubMethods.ConnectionChanged,
                new ContactReference(ContactId, selfConnectionId),
                changeType,
                cancellationToken)));
        }

        protected IEnumerable<string> GetConnectionSubscriptions()
        {
            lock (this.connectionSubscriptionsLock)
            {
                return new HashSet<string>(this.connectionSubscriptions.Keys.Select(k => k.Item1));
            }
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
            KeyValuePair<Tuple<string, string>, string[]>[] subscriptionsArray;
            lock (this.connectionSubscriptionsLock)
            {
                subscriptionsArray = this.connectionSubscriptions.ToArray();
            }

            var result = new Dictionary<Tuple<string, string>, Dictionary<string, object>>();
            foreach (var subscription in subscriptionsArray)
            {
                if (filterSubscription?.Invoke(subscription.Key.Item1, subscription.Key.Item2) == false)
                {
                    continue;
                }

                var subscriptionProperties = subscription.Value.Length > 0 && !subscription.Value.Contains("*") ? affectedProperties.Intersect(subscription.Value) : affectedProperties;
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

        private static Tuple<string, string> ToKey(string connectionId, string selfConnectionId)
        {
            return new Tuple<string, string>(connectionId, selfConnectionId);
        }
    }
}
