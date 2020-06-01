// <copyright file="Contact.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Common;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ConnectionsProperties = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Represent a registered contact instance.
    /// </summary>
    internal class Contact : ContactBase
    {
        /// <summary>
        /// Shared entity when a contact does not define any other connection properties from another endpoint outside this service.
        /// </summary>
        private static readonly Dictionary<string, ConnectionProperties> EmptyConnectionProperties = new Dictionary<string, ConnectionProperties>();

        /// <summary>
        /// Lazy creation of ContactConnectionProperties used when a real self connections is made.
        /// </summary>
        private readonly Lazy<ContactConnectionProperties> lazyContactConnectionProperties = new Lazy<ContactConnectionProperties>(() => new ContactConnectionProperties());

        /// <summary>
        /// Lazy creation of a map of connection Id with target Contact Ids.
        /// </summary>
        private readonly Lazy<Dictionary<string, HashSet<string>>> lazyTargetContactsByConnection = new Lazy<Dictionary<string, HashSet<string>>>(() => new Dictionary<string, HashSet<string>>());

        private readonly object targetContactsByConnectionLock = new object();

        /// <summary>
        /// Other connection properties maintained outside this contact.
        /// </summary>
        private MessagePackDataBuffer<Dictionary<string, ConnectionProperties>> otherConnectionPropertiesBuffer = new MessagePackDataBuffer<Dictionary<string, ConnectionProperties>>(EmptyConnectionProperties);

        public Contact(ContactService service, string contactId)
            : base(service, contactId)
        {
            Logger.LogDebug($"Contact -> contactId:{service.FormatContactId(contactId)}");
        }

        /// <summary>
        /// Report changes
        /// </summary>
        public event AsyncEventHandler<ContactChangedEventArgs> Changed;

        /// <summary>
        /// Gets all the self connections properties from this contact.
        /// </summary>
        public ConnectionsProperties SelfConnectionsProperties => ContactConnectionProperties.ConnectionsProperties;

        /// <summary>
        /// Gets a value indicating whether this contact does not have any self connection.
        /// </summary>
        internal bool IsSelfEmpty => SelfConnectionsCount == 0;

        /// <summary>
        /// Gets the number of connections maintained by this entity.
        /// </summary>
        internal int SelfConnectionsCount => GetContactConnectionProperties(cp => cp.Count, 0);

        /// <summary>
        /// Gets the self connection ids maintained by this entity.
        /// </summary>
        private ICollection<string> SelfConnectionIds => GetContactConnectionProperties(cp => cp.AllConnections, Array.Empty<string>());

        /// <summary>
        /// Gets or create the contact connection properties.
        /// </summary>
        private ContactConnectionProperties ContactConnectionProperties => this.lazyContactConnectionProperties.Value;

        /// <summary>
        /// Gets or create the target contacts entity.
        /// </summary>
        private Dictionary<string, HashSet<string>> TargetContactsByConnection => this.lazyTargetContactsByConnection.Value;

        /// <summary>
        /// Gets a value indicating whether get the contact connection are created.
        /// </summary>
        private bool IsContactConnectionPropertiesCreated => this.lazyContactConnectionProperties.IsValueCreated;

        /// <summary>
        /// Gets a value indicating whether get the target contacts are created.
        /// </summary>
        private bool IsTargetContactsByConnectionCreated => this.lazyTargetContactsByConnection.IsValueCreated;

        /// <summary>
        /// Gets the other/remote connection properties.
        /// </summary>
        private Dictionary<string, ConnectionProperties> OtherConnectionProperties => this.otherConnectionPropertiesBuffer.Data;

        /// <summary>
        /// Register a new self connection.
        /// </summary>
        /// <param name="connectionId">The connection id to start tracking.</param>
        /// <param name="initialProperties">Optional properties associated with this self connection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        public async Task RegisterSelfAsync(
            string connectionId,
            Dictionary<string, object> initialProperties,
            CancellationToken cancellationToken)
        {
            // notify connection added
            await NotifyConnectionChangedAsync(connectionId, ConnectionChangeType.Added, cancellationToken);

            ContactConnectionProperties.AddConnection(connectionId);
            if (initialProperties != null)
            {
                await UpdatePropertiesAsync(connectionId, initialProperties, ContactUpdateType.Registration, mergeProperties: true, cancellationToken);
            }
        }

        /// <summary>
        /// Add target contacts to track.
        /// Note: this will be used later when this contact unregister.
        /// </summary>
        /// <param name="connectionId">The connection associated with this call.</param>
        /// <param name="targetContactIds">The contacts ids to track.</param>
        public void AddTargetContacts(string connectionId, string[] targetContactIds)
        {
            TargetContactsByConnection.AddOrUpdate(
                connectionId,
                hashSet => hashSet.UnionWith(targetContactIds),
                this.targetContactsByConnectionLock);
        }

        /// <summary>
        /// Remove tracked target contacts.
        /// </summary>
        /// <param name="connectionId">The connection id to remove.</param>
        /// <param name="targetContactIds">The contacts ids to remove.</param>
        public void RemovedTargetContacts(string connectionId, string[] targetContactIds)
        {
            lock (this.targetContactsByConnectionLock)
            {
                if (TargetContactsByConnection.TryGetValue(connectionId, out var hashSet))
                {
                    foreach (var targetContactId in targetContactIds)
                    {
                        hashSet.Remove(targetContactId);
                    }
                }
            }
        }

        /// <summary>
        /// Return the target contacts this connection is tracking.
        /// </summary>
        /// <param name="connectionId">The connection id.</param>
        /// <returns>Array of contacts ids.</returns>
        public string[] GetTargetContacts(string connectionId)
        {
            lock (this.targetContactsByConnectionLock)
            {
                if (IsTargetContactsByConnectionCreated && TargetContactsByConnection.TryGetValue(connectionId, out var hashSet))
                {
                    return hashSet.ToArray();
                }
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Create contact subscription that will be tracked to later report changes.
        /// </summary>
        /// <param name="connectionId">The associated connection id to report.</param>
        /// <param name="selfConnectionId">Optional self connection id.</param>
        /// <param name="propertyNames">List of properties that this connection is interested.</param>
        /// <returns>List of current property values for the properties being asked.</returns>
        public Dictionary<string, object> CreateSubcription(string connectionId, string selfConnectionId, string[] propertyNames)
        {
            AddSubcriptionProperties(connectionId, selfConnectionId, propertyNames);

            if (propertyNames.Contains("*"))
            {
                return GetAllProperties(selfConnectionId);
            }
            else
            {
                var currentValues = new Dictionary<string, object>();
                foreach (var propertyName in propertyNames)
                {
                    currentValues[propertyName] = GetPropertyValue(selfConnectionId, propertyName);
                }

                return currentValues;
            }
        }

        /// <summary>
        /// Update the property value of this contact.
        /// </summary>
        /// <param name="connectionId">The connection id that is updating the values.</param>
        /// <param name="updateProperties">The updated properties.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        public Task UpdatePropertiesAsync(
            string connectionId,
            Dictionary<string, object> updateProperties,
            CancellationToken cancellationToken)
        {
            return UpdatePropertiesAsync(connectionId, updateProperties, ContactUpdateType.UpdateProperties, mergeProperties: true, cancellationToken);
        }

        /// <summary>
        /// Send receive message notifications.
        /// </summary>
        /// <param name="fromContact">The contact reference who is sending the message.</param>
        /// <param name="messageType">Message type.</param>
        /// <param name="body">Payload content of the message.</param>
        /// <param name="targetConnectionId">Optional connection id to target.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        public async Task SendReceiveMessageAsync(
            ContactReference fromContact,
            string messageType,
            object body,
            string targetConnectionId,
            CancellationToken cancellationToken)
        {
            var sendTasks = new List<Task>();

            Func<ContactReference, Task> sendReceiveMessageAsync = (contactRef) => NotifyReceiveMessageAsync(contactRef, fromContact, messageType, body, cancellationToken);
            if (!string.IsNullOrEmpty(targetConnectionId))
            {
                sendTasks.Add(sendReceiveMessageAsync(new ContactReference(ContactId, targetConnectionId)));
            }
            else
            {
                foreach (var selfConnectionId in SelfConnectionIds)
                {
                    sendTasks.Add(sendReceiveMessageAsync(new ContactReference(ContactId, selfConnectionId)));
                }
            }

            await Task.WhenAll(sendTasks);
        }

        /// <summary>
        /// Return true if this contact would be able to deliver a message to its self connections end points.
        /// </summary>
        /// <param name="targetConnectionId">The desired target connection or null if delivery will be for all self connections.</param>
        /// <returns>true if this contact can send a message.</returns>
        public bool CanSendMessage(string targetConnectionId)
        {
            return (string.IsNullOrEmpty(targetConnectionId) && !IsSelfEmpty) ||
                (!string.IsNullOrEmpty(targetConnectionId) && GetContactConnectionProperties(cp => cp.HasConnection(targetConnectionId), false));
        }

        /// <summary>
        /// Remove a self connection from this contact.
        /// </summary>
        /// <param name="connectionId">The self connection id that is being removed.</param>
        /// <param name="affectedPropertiesTask">Optional task to invoke with the affected properties.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Completion task.</returns>
        public async Task RemoveSelfConnectionAsync(
            string connectionId,
            Func<IEnumerable<string>, Task> affectedPropertiesTask,
            CancellationToken cancellationToken)
        {
            // If the connection was dropped before stop now
            if (!GetContactConnectionProperties(cp => cp.HasConnection(connectionId), false))
            {
                return;
            }

            lock (this.targetContactsByConnectionLock)
            {
                TargetContactsByConnection.Remove(connectionId);
            }

            IEnumerable<Task> sendTasks = null;
            IEnumerable<string> affectedProperties;
            if (affectedPropertiesTask == null)
            {
                affectedProperties = ContactConnectionProperties.RemoveConnectionProperties(connectionId);
                sendTasks = GetSubscriptionsNotityProperties(affectedProperties).Select(kvp => NotifyUpdateValuesAsync(kvp.Key, kvp.Value, connectionId, cancellationToken));
            }
            else
            {
                // compute all properties that would be affected by this connection id
                if (ContactConnectionProperties.TryGetProperties(connectionId, out var properties))
                {
                    affectedProperties = properties.Keys;
                }
                else
                {
                    affectedProperties = Array.Empty<string>();
                }

                // compute all property values for the properties
                var beforeRemoveNotifyProperties = GetSubscriptionsNotityProperties(affectedProperties);

                // now remove the connection properties for this connection id
                affectedProperties = ContactConnectionProperties.RemoveConnectionProperties(connectionId);
                if (affectedProperties.Any())
                {
                    await affectedPropertiesTask(affectedProperties);

                    var afterRemoveNotifyProperties = GetSubscriptionsNotityProperties(affectedProperties);
                    sendTasks = afterRemoveNotifyProperties.Where(kvp =>
                    {
                        return !(beforeRemoveNotifyProperties.TryGetValue(kvp.Key, out var notifyProperties)
                                    && notifyProperties.EqualsProperties(kvp.Value));
                    }).Select(kvp => NotifyUpdateValuesAsync(kvp.Key, kvp.Value, connectionId, cancellationToken));
                }
            }

            // Note: notice we are firing the contact event to trigger our backplane provider mechanism
            // that will eventuall catch any exception error and so we will mosty past the 'await' statement
            // fire RemoveSelf change type
            await FireChangeAsync(
                connectionId,
                affectedProperties.ToDictionary(p => p, p => new PropertyValue(null, default)),
                ContactUpdateType.Unregister);

            // Both next notiifications could eventuall trigger exceptions that may interrupt the call to this method
            if (sendTasks?.Any() == true)
            {
                await Task.WhenAll(sendTasks);
            }

            // notify connection removed
            await NotifyConnectionChangedAsync(connectionId, ConnectionChangeType.Removed, cancellationToken);
        }

        /// <summary>
        /// Return the aggregated properties of all the self connections that has define property values.
        /// </summary>
        /// <returns>The aggregated properties.</returns>
        public Dictionary<string, object> GetAggregatedProperties()
        {
            return GetAggregatedProperties(null);
        }

        /// <summary>
        /// Returns live connections for this contact entity.
        /// </summary>
        /// <returns>Aggregated dictionary of connection properties.</returns>
        internal Dictionary<string, ConnectionProperties> GetSelfConnections()
        {
            return GetContactConnectionProperties(cp => cp.AllConnectionValues, Array.Empty<KeyValuePair<string, ConnectionProperties>>())
                .Union(OtherConnectionProperties).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        internal void SetOtherConnectionProperties(Dictionary<string, ConnectionProperties> otherConnectionProperties)
        {
            this.otherConnectionPropertiesBuffer.Data = otherConnectionProperties;
        }

        /// <summary>
        /// Process aggregated coming from a backplane provider.
        /// </summary>
        /// <param name="contactDataChanged">The backplane data that changed.</param>
        /// <param name="affectedProperties">Affected properties.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        internal async Task OnContactChangedAsync(
            ContactDataChanged<Dictionary<string, ConnectionProperties>> contactDataChanged,
            IEnumerable<string> affectedProperties,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginContactReferenceScope(ContactServiceScopes.MethodContactOnContactChanged, contactDataChanged.ContactId, contactDataChanged.ConnectionId, Service.FormatProvider))
            {
                Logger.LogDebug($"serviceId:{contactDataChanged.ServiceId} type:{contactDataChanged.ChangeType}");
            }

            // merge the contact data
            SetOtherConnectionProperties(contactDataChanged.Data);

            if (contactDataChanged.ChangeType == ContactUpdateType.Registration || contactDataChanged.ChangeType == ContactUpdateType.Unregister)
            {
                await NotifyConnectionChangedAsync(
                    contactDataChanged.ConnectionId,
                    contactDataChanged.ChangeType == ContactUpdateType.Registration ? ConnectionChangeType.Added : ConnectionChangeType.Removed,
                    cancellationToken);
            }

            // notify the changes to our subscribers and self
            await UpdatePropertiesAsync(
                contactDataChanged.ConnectionId,
                this.GetAggregatedProperties(affectedProperties),
                ContactUpdateType.None,
                mergeProperties: false,
                cancellationToken);
        }

        private IEnumerable<Task> GetSendUpdateValues(
            string connectionId,
            IEnumerable<string> affectedProperties,
            CancellationToken cancellationToken)
        {
            return GetSendUpdateValues(
                connectionId,
                affectedProperties,
                (selfConnectionId, propertyName) => GetPropertyValue(selfConnectionId, propertyName),
                (notifyConnectionId, selfConnectionId) => (!GetContactConnectionProperties(cp => cp.HasConnection(notifyConnectionId), false) && selfConnectionId == null) || selfConnectionId == connectionId,
                cancellationToken);
        }

        private Task NotifyConnectionChangedAsync(
            string connectionId,
            ConnectionChangeType changeType,
            CancellationToken cancellationToken)
        {
            return NotifyConnectionChangedAsync(SelfConnectionIds, connectionId, changeType, cancellationToken);
        }

        private IEnumerable<ConnectionProperties> GetAllConnectionProperties()
        {
            return GetContactConnectionProperties(cp => cp.AllConnectionProperties, Array.Empty<ConnectionProperties>())
                .Union(OtherConnectionProperties.Values);
        }

        private Dictionary<string, object> GetAggregatedProperties(IEnumerable<string> affectedProperties)
        {
            var connectionProperties = GetAllConnectionProperties();
            if (affectedProperties != null)
            {
                var nullProperties = affectedProperties.ToDictionary(p => p, p => new PropertyValue(null, DateTime.MinValue));
                connectionProperties = connectionProperties.Union(new ConnectionProperties[] { nullProperties });
            }

            var properties = ContactDataHelpers.GetAggregatedProperties(connectionProperties);
            if (affectedProperties != null)
            {
                // restrict to only the affected properties
                properties = properties.Where(kvp => affectedProperties.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            return properties;
        }

        private async Task UpdatePropertiesAsync(
            string connectionId,
            Dictionary<string, object> updateProperties,
            ContactUpdateType changedType,
            bool mergeProperties,
            CancellationToken cancellationToken)
        {
            var lastUpdated = DateTime.UtcNow;
            if (mergeProperties)
            {
                // merge properties
                foreach (var item in updateProperties)
                {
                    ContactConnectionProperties.MergeProperty(connectionId, item.Key, item.Value, lastUpdated);
                }
            }

            var sendTasks = new List<Task>();
            sendTasks.AddRange(GetSendUpdateValues(
                connectionId,
                updateProperties.Keys,
                cancellationToken));

            // Notify self
            foreach (var selfConnectionId in SelfConnectionIds)
            {
                sendTasks.Add(NotifyUpdateValuesAsync(new Tuple<string, string>(selfConnectionId, null), updateProperties, connectionId, cancellationToken));
            }

            await Task.WhenAll(sendTasks);

            if (changedType != ContactUpdateType.None)
            {
                // fire
                await FireChangeAsync(
                    connectionId,
                    updateProperties.ToDictionary(kvp => kvp.Key, kvp => new PropertyValue(kvp.Value, lastUpdated)),
                    changedType);
            }
        }

        private Dictionary<Tuple<string, string>, Dictionary<string, object>> GetSubscriptionsNotityProperties(
            IEnumerable<string> affectedProperties)
        {
            return GetSubscriptionsNotityProperties(
                affectedProperties,
                (selfConnectionId, propertyName) => GetPropertyValue(selfConnectionId, propertyName),
                null);
        }

        private async Task FireChangeAsync(
            string connectionId,
            ConnectionProperties properties,
            ContactUpdateType changeType)
        {
            await Changed?.InvokeAsync(this, new ContactChangedEventArgs(connectionId, properties, changeType));
        }

        /**
         * Return a property value that would include both self maintained value and also externally
         * maintained outside this entity
         */
        private object GetPropertyValue(string connectionId, string propertyName)
        {
            if (!string.IsNullOrEmpty(connectionId))
            {
                PropertyValue pv;
                if ((IsContactConnectionPropertiesCreated && ContactConnectionProperties.TryGetProperties(connectionId, out var properties) &&
                    properties.TryGetValue(propertyName, out pv)) ||
                    (OtherConnectionProperties.TryGetValue(connectionId, out var connProperties) &&
                    connProperties.TryGetValue(propertyName, out pv)))
                {
                    return pv.Value;
                }
            }
            else
            {
                return GetAllConnectionProperties().Select(c =>
                {
                    if (c.TryGetValue(propertyName, out var pv))
                    {
                        return pv;
                    }

                    return default;
                }).GetLatestValue();
            }

            return null;
        }

        private Dictionary<string, object> GetAllProperties(string connectionId)
        {
            if (!string.IsNullOrEmpty(connectionId))
            {
                if (IsContactConnectionPropertiesCreated && ContactConnectionProperties.TryGetProperties(connectionId, out var properties))
                {
                    return properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
                }
                else if (OtherConnectionProperties.TryGetValue(connectionId, out var connProperties))
                {
                    return connProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
                }
                else
                {
                    return new Dictionary<string, object>();
                }
            }
            else
            {
                return ContactDataHelpers.GetAggregatedProperties(GetAllConnectionProperties());
            }
        }

        private T GetContactConnectionProperties<T>(Func<ContactConnectionProperties, T> callback, Func<T> defaultCallback)
        {
            if (this.lazyContactConnectionProperties.IsValueCreated)
            {
                return callback(this.lazyContactConnectionProperties.Value);
            }

            return defaultCallback();
        }

        private T GetContactConnectionProperties<T>(Func<ContactConnectionProperties, T> callback, T defaultValue)
        {
            return GetContactConnectionProperties<T>(callback, () => defaultValue);
        }
    }
}
