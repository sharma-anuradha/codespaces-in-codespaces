using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Contact base class 
    /// </summary>
    internal class ContactBase
    {
        private readonly PresenceService service;

        /// <summary>
        /// Map of connection Id <-> properties
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentHashSet<string>> connectionSubscriptions = new ConcurrentDictionary<string, ConcurrentHashSet<string>>();

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
        /// Add a subscription to this instance
        /// </summary>
        /// <param name="connectionId">The connection id to track</param>
        /// <param name="propertyNames">Properties to track</param>
        public void AddSubcription(string connectionId, string[] propertyNames)
        {
            this.connectionSubscriptions.AddOrUpdate(connectionId, propertyNames);
        }

        /// <summary>
        /// Remove subscription properties from this instance
        /// </summary>
        /// <param name="connectionId">The existing tracked connection id</param>
        /// <param name="propertyNames">The properties to remove from the existing subscription</param>
        /// <returns></returns>
        public bool RemoveSubcriptionProperties(string connectionId, string[] propertyNames)
        {
            ConcurrentHashSet<string> properties;
            if (this.connectionSubscriptions.TryGetValue(connectionId, out properties))
            {
                foreach (var name in propertyNames)
                {
                    properties.TryRemove(name);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove a subscription being mantained by this contact
        /// </summary>
        /// <param name="connectionId">The tracked connection id</param>
        public void RemoveSubscription(string connectionId)
        {
            this.connectionSubscriptions.TryRemove(connectionId, out var properties);
        }

        /// <summary>
        /// Notify updated properties for this contact
        /// </summary>
        /// <param name="updateProperties">The properties to report changes </param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendUpdatePropertiesAsync(
            Dictionary<string, object> updateProperties,
            CancellationToken cancellationToken)
        {
            await Task.WhenAll(GetSendUpdateProperties(updateProperties, cancellationToken));
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

        protected IEnumerable<Task> GetSendUpdateProperties(
            Dictionary<string, object> updateProperties,
            CancellationToken cancellationToken)
        {
            return GetSendUpdateValues(updateProperties, (propertyName) => updateProperties[propertyName], cancellationToken);
        }

        protected Task SendUpdateValuesAsync(string connectionId, Dictionary<string, object> notifyProperties, CancellationToken cancellationToken)
        {
            var client = Client(connectionId);
            if (client != null)
            {
                Logger.LogDebug($"Notify->{Methods.UpdateValues} connectionId:{connectionId} contactId:{ContactId} notifyProperties:{notifyProperties.ConvertToString()}");
                return client.SendAsync(Methods.UpdateValues, ContactId, notifyProperties, cancellationToken);
            }

            return Task.CompletedTask;
        }

        protected Task SendReceiveMessageAsync(string connectionId, string contactId, string fromContactId, string messageType, JToken body, CancellationToken cancellationToken)
        {
            var client = Client(connectionId);
            if (client != null)
            {
                Logger.LogDebug($"Notify->{Methods.ReceiveMessage} connectionId:{connectionId} contactId:{contactId} fromContactId:{fromContactId} messageType:{messageType} body:{body}");
                return client.SendAsync(Methods.ReceiveMessage, contactId, fromContactId, messageType, body, cancellationToken);
            }

            return Task.CompletedTask;
        }

        protected IEnumerable<Task> GetSendUpdateValues(
            Dictionary<string, object> updateProperties,
            Func<string, object> resolvePropertyValue,
            CancellationToken cancellationToken)
        {
            return GetSubscriptionsNotifyProperties(
                (name) => updateProperties.ContainsKey(name),
                resolvePropertyValue).Select(kvp => SendUpdateValuesAsync(kvp.Key, kvp.Value, cancellationToken));
        }

        protected Dictionary<string, Dictionary<string, object>> GetSubscriptionsNotityProperties(
            HashSet<string> affectedProperties,
            Func<string, object> resolvePropertyValue)
        {
            var result = new Dictionary<string, Dictionary<string, object>>();
            foreach (var subscription in this.connectionSubscriptions)
            {
                var subscriptionProperties = affectedProperties.Intersect(subscription.Value.Values);
                if (subscriptionProperties.Any())
                {
                    var notifyProperties = new Dictionary<string, object>();
                    foreach (var propertyName in subscriptionProperties)
                    {
                        notifyProperties[propertyName] = resolvePropertyValue(propertyName);
                    }

                    result[subscription.Key] = notifyProperties;
                }
            }

            return result;
        }

        private Dictionary<string, Dictionary<string, object>> GetSubscriptionsNotifyProperties(
            Func<string, bool> filterProperty,
            Func<string, object> resolvePropertyValue)
        {
            var result = new Dictionary<string, Dictionary<string, object>>();
            foreach (var subscription in this.connectionSubscriptions)
            {
                var notifyProperties = new Dictionary<string, object>();
                foreach (var propertyName in subscription.Value.Values)
                {
                    if (filterProperty(propertyName))
                    {
                        notifyProperties[propertyName] = resolvePropertyValue(propertyName);
                    }
                }

                if (notifyProperties.Count > 0)
                {
                    result[subscription.Key] = notifyProperties;
                }
            }

            return result;
        }
    }
}
