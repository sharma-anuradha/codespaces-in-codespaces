using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Event to send when the contact has been changed
    /// </summary>
    internal class ContactChangedEventArgs : EventArgs
    {
        internal ContactChangedEventArgs(
            string connectionId,
            ConnectionProperties properties,
            ContactUpdateType changeType)
        {
            ConectionId = connectionId;
            Properties = properties;
            ChangeType = changeType;
        }

        public string ConectionId { get; }

        public ConnectionProperties Properties { get; }
        public ContactUpdateType ChangeType { get; }
    }

    /// <summary>
    /// Represent a registered contact instance
    /// </summary>
    internal class Contact : ContactBase
    {
        /// <summary>
        /// Map of connection Id <-> target Contact Ids
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentHashSet<string>> targetContactsByConnection = new ConcurrentDictionary<string, ConcurrentHashSet<string>>();

        /// <summary>
        /// Properties maintained by each of the live connections
        /// Key: connection Id
        /// Value: A dictionary property info structure with the value and the timestamp when it was updated
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PropertyValue>> selfConnectionProperties = new ConcurrentDictionary<string, ConcurrentDictionary<string, PropertyValue>>();

        /// <summary>
        /// Other connection properties maintained outside this contact
        /// </summary>
        private Dictionary<string, ConnectionProperties> otherConnectionProperties = new Dictionary<string, ConnectionProperties>();

        /// <summary>
        /// If this contact does not have any self connection
        /// </summary>
        internal bool IsSelfEmpty => this.selfConnectionProperties.Count == 0;

        /// <summary>
        /// Number of connections maintained by this entity
        /// </summary>
        internal int SelfConnectionsCount => this.selfConnectionProperties.Count;

        /// <summary>
        /// Return the self connection ids maintained by this entity
        /// </summary>
        private ICollection<string> SelfConnectionIds => this.selfConnectionProperties.Keys;

        public Contact(PresenceService service, string contactId)
            : base(service, contactId)
        {
            Logger.LogDebug($"Contact -> contactId:{contactId}");
        }

        /// <summary>
        /// Report changes
        /// </summary>
        public event AsyncEventHandler<ContactChangedEventArgs> Changed;

        /// <summary>
        /// Register a new self connection
        /// </summary>
        /// <param name="connectionId">The connection id to start tracking</param>
        /// <param name="initialProperties">Optional properties associated with this self connection</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RegisterSelfAsync(
            string connectionId,
            Dictionary<string, object> initialProperties,
            CancellationToken cancellationToken)
        {
            // notify connection added
            await NotifyConnectionChangedAsync(connectionId, ConnectionChangeType.Added, cancellationToken);

            this.selfConnectionProperties[connectionId] = new ConcurrentDictionary<string, PropertyValue>();

            if (initialProperties != null)
            {
                await UpdatePropertiesAsync(connectionId, initialProperties, ContactUpdateType.Registration, mergeProperties: true, cancellationToken);
            }
        }

        /// <summary>
        /// Add target contacts to track.
        /// Note: this will be used later when this contact unregister
        /// </summary>
        /// <param name="connectionId">The connection associated with this call</param>
        /// <param name="targetContactIds">The contacts ids to track</param>
        public void AddTargetContacts(string connectionId, string[] targetContactIds)
        {
            this.targetContactsByConnection.AddOrUpdate(connectionId, targetContactIds);
        }

        /// <summary>
        /// Remove tracked target contacts
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="targetContactIds">The contacts ids to remove</param>
        public void RemovedTargetContacts(string connectionId, string[] targetContactIds)
        {
            this.targetContactsByConnection.RemoveValues(connectionId, targetContactIds);
        }

        /// <summary>
        /// Return the list target contacts this connection is tracking
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public string[] GetTargetContacts(string connectionId)
        {
            ConcurrentHashSet<string> targetContactIds;
            if (this.targetContactsByConnection.TryGetValue(connectionId, out targetContactIds))
            {
                return targetContactIds.Values.ToArray();
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Create contact subscription that will be tracked to later report changes
        /// </summary>
        /// <param name="connectionId">The associated connection id to report</param>
        /// <param name="selfConnectionId">Optional self connection id</param>
        /// <param name="propertyNames">List of properties that this connection is interested</param>
        /// <returns>List of current property values for the properties being asked</returns>
        public Dictionary<string, object> CreateSubcription(string connectionId, string selfConnectionId, string[] propertyNames)
        {
            AddSubcriptionProperties(connectionId, selfConnectionId, propertyNames);

            var currentValues = new Dictionary<string, object>();
            foreach (var propertyName in propertyNames)
            {
                currentValues[propertyName] = GetPropertyValue(selfConnectionId, propertyName);
            }

            return currentValues;
        }

        /// <summary>
        /// Update the property value of this contact
        /// </summary>
        /// <param name="connectionId">The connection id that is updating the values</param>
        /// <param name="updateProperties">The updated properties</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task UpdatePropertiesAsync(
            string connectionId,
            Dictionary<string, object> updateProperties,
            CancellationToken cancellationToken)
        {
            return UpdatePropertiesAsync(connectionId, updateProperties, ContactUpdateType.UpdateProperties, mergeProperties: true, cancellationToken);
        }

        /// <summary>
        /// Send receive message notifications
        /// </summary>
        /// <param name="fromContact">The contact reference who is sending the message</param>
        /// <param name="messageType">Message type</param>
        /// <param name="body">Payload content of the message</param>
        /// <param name="targetConnectionId">Optional connection id to target</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
        /// Remove a self connection from this contact
        /// </summary>
        /// <param name="connectionId">The self connection id that is being removed</param>
        /// <param name="affectedPropertiesTask">Optional task to invoke with the affected properties</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RemoveSelfConnectionAsync(
            string connectionId,
            Func<IEnumerable<string>, Task> affectedPropertiesTask,
            CancellationToken cancellationToken)
        {
            // If the connection was dropped before stop now
            if (!this.selfConnectionProperties.ContainsKey(connectionId))
            {
                return;
            }

            this.targetContactsByConnection.TryRemove(connectionId, out var removed);

            // notify connection removed
            await NotifyConnectionChangedAsync(connectionId, ConnectionChangeType.Removed, cancellationToken);

            IEnumerable<Task> sendTasks = null;
            IEnumerable<string> affectedProperties;
            if (affectedPropertiesTask == null)
            {
                affectedProperties = RemoveConnectionProperties(connectionId);
                sendTasks = GetSubscriptionsNotityProperties(affectedProperties).Select(kvp => NotifyUpdateValuesAsync(kvp.Key, kvp.Value, connectionId, cancellationToken));
            }
            else
            {
                // compute all properties that would be affected by this connection id
                if (this.selfConnectionProperties.TryGetValue(connectionId, out var properties))
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
                affectedProperties = RemoveConnectionProperties(connectionId);
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

            if (sendTasks?.Any() == true)
            {
                await Task.WhenAll(sendTasks);
            }

            // fire RemoveSelf change type
            await FireChangeAsync(
                connectionId,
                affectedProperties.ToDictionary(p => p, p => new PropertyValue(null, default)),
                ContactUpdateType.Unregister);
        }

        /// <summary>
        /// Return the aggregated properties of all the self connections that has define property values
        /// </summary>
        /// <returns>The aggregated properties</returns>
        public Dictionary<string, object> GetAggregatedProperties()
        {
            return GetAggregatedProperties(null);
        }

        /// <summary>
        /// Returns live connections for this contact entity
        /// </summary>
        /// <returns></returns>
        internal Dictionary<string, ConnectionProperties> GetSelfConnections()
        {
            return this.selfConnectionProperties.Select(kvp => new KeyValuePair<string, ConnectionProperties>(kvp.Key, (ConnectionProperties)kvp.Value))
                .Union(otherConnectionProperties).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /**
         * Extract the connections that are not 'self'
         */
        internal void SetOtherContactData(ContactDataInfo contactDataInfo)
        {
            var propertyKeys = contactDataInfo.Values
                .SelectMany(i => i.Values).SelectMany(p => p.Keys);

            this.otherConnectionProperties = contactDataInfo.Values
                .SelectMany(i => i)
                .Where(p => !this.selfConnectionProperties.ContainsKey(p.Key))
                .ToDictionary(p => p.Key, p => p.Value);
        }

        /// <summary>
        /// Process aggregated coming from a backplane provider
        /// </summary>
        /// <param name="contactDataChanged">The backplane data that changed</param>
        /// <param name="affectedProperties">Affected properties</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal async Task OnContactChangedAsync(
            ContactDataChanged<ContactDataInfo> contactDataChanged,
            IEnumerable<string> affectedProperties,
            CancellationToken cancellationToken)
        {
            // merge the contact data
            SetOtherContactData(contactDataChanged.Data);

            if (contactDataChanged.Type == ContactUpdateType.Registration || contactDataChanged.Type == ContactUpdateType.Unregister)
            {
                await NotifyConnectionChangedAsync(
                    contactDataChanged.ConnectionId,
                    contactDataChanged.Type == ContactUpdateType.Registration ? ConnectionChangeType.Added : ConnectionChangeType.Removed,
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
                (notifyConnectionId, selfConnectionId) => (!this.selfConnectionProperties.ContainsKey(notifyConnectionId) && selfConnectionId == null) || selfConnectionId == connectionId,
                cancellationToken);
        }

        private Task NotifyConnectionChangedAsync(
            string connectionId,
            ConnectionChangeType changeType,
            CancellationToken cancellationToken)
        {
            return NotifyConnectionChangedAsync(SelfConnectionIds, connectionId, changeType, cancellationToken);
        }

        private Dictionary<string, object> GetAggregatedProperties(IEnumerable<string> affectedProperties)
        {
            var connectionProperties = this.selfConnectionProperties.Values.Cast<ConnectionProperties>()
                .Union(this.otherConnectionProperties.Values);

            if (affectedProperties != null)
            {
                var nullProperties = affectedProperties.ToDictionary(p => p, p => new PropertyValue(null, DateTime.MinValue));
                connectionProperties = connectionProperties.Union(new ConnectionProperties[] { nullProperties });
            }

            return ContactDataHelpers.GetAggregatedProperties(connectionProperties);
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
                    MergeProperty(connectionId, item.Key, item.Value, lastUpdated);
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

        private void MergeProperty(string connectionId, string propertyName, object value, DateTime updated)
        {
            var propertyValue = new PropertyValue(value, updated);
            this.selfConnectionProperties.AddOrUpdate(
                connectionId,
                (properties) => properties.AddOrUpdate(propertyName, propertyValue, (k, v) => propertyValue));
        }

        /**
         * Return a property value that would include both self maintained value and also exteranlly
         * maintained outside this entity
         */
        private object GetPropertyValue(string connectionId, string propertyName)
        {
            if (!string.IsNullOrEmpty(connectionId))
            {
                PropertyValue pv;
                if ((this.selfConnectionProperties.TryGetValue(connectionId, out var properties) &&
                    properties.TryGetValue(propertyName, out pv)) ||
                    (this.otherConnectionProperties.TryGetValue(connectionId, out var connProperties) &&
                    connProperties.TryGetValue(propertyName, out pv)))
                {
                    return pv.Value;
                }
            }
            else
            {
                return this.selfConnectionProperties.Values.Union(this.otherConnectionProperties.Values).Select(c =>
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

        private IEnumerable<string> RemoveConnectionProperties(string connectionId)
        {
            if (this.selfConnectionProperties.TryRemove(connectionId, out var properties))
            {
                return properties.Keys;
            }

            return Array.Empty<string>();
        }
    }
}
