using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Contact changed types
    /// </summary>
    internal enum ContactChangedType
    {
        InitialProperties,
        UpdateProperties,
    }

    /// <summary>
    /// Event to send when the contact has been changed
    /// </summary>
    internal class ContactChangedEventArgs : EventArgs
    {
        internal ContactChangedEventArgs(ContactChangedType property)
        {
            Property = property;
        }

        public ContactChangedType Property { get; }
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
        /// Properties mantained by each of the live connections
        /// Key: property name
        /// Value: A property info structure with the value and the timestamp when it was updated
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PropertyValue>> properties = new ConcurrentDictionary<string, ConcurrentDictionary<string, PropertyValue>>();

        /// <summary>
        /// Who are the live connections refering to this self contact
        /// </summary>
        private readonly ConcurrentHashSet<string> selfConnections = new ConcurrentHashSet<string>();

        /// <summary>
        /// If this contact does not have any self connection
        /// </summary>
        internal bool IsSelfEmpty => this.selfConnections.Count == 0;

        internal int SelfConnectionsCount => this.selfConnections.Count;

        public Contact(PresenceService service,string contactId)
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
        public Task RegisterSelfAsync(string connectionId, Dictionary<string, object> initialProperties, CancellationToken cancellationToken)
        {
            this.selfConnections.Add(connectionId);
            if (initialProperties != null)
            {
                return UpdatePropertiesAsync(connectionId, initialProperties, ContactChangedType.InitialProperties, cancellationToken);
            }

            return Task.CompletedTask;
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
        /// <param name="propertyNames">List of properties that this connection is interested</param>
        /// <returns>List of current property values for the properties being asked</returns>
        public Dictionary<string, object> CreateSubcription(string connectionId, string[] propertyNames)
        {
            AddSubcription(connectionId, propertyNames);

            var currentValues = new Dictionary<string, object>();
            foreach (var propertyName in propertyNames)
            {
                currentValues[propertyName] = GetAggregatedProperty(propertyName);
            }

            return currentValues;
        }

        /// <summary>
        /// Update the property value sof this contact
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
            return UpdatePropertiesAsync(connectionId, updateProperties, ContactChangedType.UpdateProperties, cancellationToken);
        }

        /// <summary>
        /// Send receive message notifications
        /// </summary>
        /// <param name="fromContactId">The contact id who is sending the message</param>
        /// <param name="messageType">Message type</param>
        /// <param name="body">Payload content of the message</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendReceiveMessageAsync(
            string fromContactId,
            string messageType,
            JToken body,
            CancellationToken cancellationToken)
        {
            var sendTasks = new List<Task>();
            foreach (var selfConnectionId in this.selfConnections.Values)
            {
                sendTasks.Add(SendReceiveMessageAsync(selfConnectionId, ContactId, fromContactId, messageType, body, cancellationToken));
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
            this.selfConnections.TryRemove(connectionId);
            this.targetContactsByConnection.TryRemove(connectionId, out var removed);

            IEnumerable<Task> sendTasks = null;
            HashSet<string> affectedProperties;
            if (affectedPropertiesTask == null)
            {
                affectedProperties = RemoveConnectionProperties(connectionId);
                sendTasks = GetSubscriptionsNotityProperties(affectedProperties).Select(kvp => SendUpdateValuesAsync(kvp.Key, kvp.Value, cancellationToken));
            }
            else
            {
                // compute all properties that would be affected by this connection id
                affectedProperties = new HashSet<string>(this.properties.Where(kvp => kvp.Value.ContainsKey(connectionId)).Select(kvp => kvp.Key));

                // compute all property values for the properties
                var beforeRemoveNotifyProperties = GetSubscriptionsNotityProperties(affectedProperties);

                // now remove the connection properties for this connection id
                affectedProperties = RemoveConnectionProperties(connectionId);
                if (affectedProperties.Count > 0)
                {
                    await affectedPropertiesTask(affectedProperties);

                    var afterRemoveNotifyProperties = GetSubscriptionsNotityProperties(affectedProperties);
                    sendTasks = afterRemoveNotifyProperties.Where(kvp =>
                    {
                        return !(beforeRemoveNotifyProperties.TryGetValue(kvp.Key, out var notifyProperties)
                                    && notifyProperties.EqualsProperties(kvp.Value));
                    }).Select(kvp => SendUpdateValuesAsync(kvp.Key, kvp.Value, cancellationToken));
                }
            }

            if (sendTasks?.Any() == true)
            {
                await Task.WhenAll(sendTasks);
            }
            // fire UpdateProperties type
            await FireChangeAsync(ContactChangedType.UpdateProperties);
        }

        /// <summary>
        /// Return the aggregated properties of all the self connections that has define property values
        /// </summary>
        /// <returns>The aggregated properties</returns>
        public Dictionary<string, object> GetAggregatedProperties()
        {
            return this.properties.ToDictionary(kvp => kvp.Key, kvp => GetAggregatedProperty(kvp.Key));
        }

        private async Task UpdatePropertiesAsync(
            string connectionId,
            Dictionary<string, object> updateProperties,
            ContactChangedType changedType,
            CancellationToken cancellationToken)
        {
            // merge properties
            foreach (var item in updateProperties)
            {
                MergeProperty(connectionId, item.Key, item.Value);
            }

            var sendTasks = new List<Task>();
            sendTasks.AddRange(GetSendUpdateValues(
                updateProperties,
                (propertyName) => GetAggregatedProperty(propertyName),
                cancellationToken));

            // Notify self
            foreach (var selfConnectionId in this.selfConnections.Values)
            {
                sendTasks.Add(SendUpdateValuesAsync(selfConnectionId, updateProperties, cancellationToken));
            }

            await Task.WhenAll(sendTasks);

            // fire 
            await FireChangeAsync(changedType);
        }

        private Dictionary<string, Dictionary<string, object>> GetSubscriptionsNotityProperties(
            HashSet<string> affectedProperties)
        {
            return GetSubscriptionsNotityProperties(
                affectedProperties,
                (propertyName) => GetAggregatedProperty(propertyName));
        }

        private async Task FireChangeAsync(ContactChangedType property)
        {
            await Changed?.InvokeAsync(this, new ContactChangedEventArgs(property));
        }

        private void MergeProperty(string connectionId, string propertyName, object value)
        {
            var propertyValue = new PropertyValue(value, DateTime.Now);
            this.properties.AddOrUpdate(
                propertyName,
                (connProperties) => connProperties.AddOrUpdate(connectionId, propertyValue, (k,v) => propertyValue));
        }

        private object GetAggregatedProperty(string propertyName)
        {
            if (this.properties.TryGetValue(propertyName, out var connProperties) && connProperties.Count > 0)
            {
                return connProperties.OrderByDescending(kvp => kvp.Value.Updated).FirstOrDefault().Value.Value;
            }

            return null;
        }

        private HashSet<string> RemoveConnectionProperties(string connectionId)
        {
            var affectedProperties = new HashSet<string>();
            foreach(var kvp in this.properties)
            {
                if (kvp.Value.TryRemove(connectionId, out var value))
                {
                    affectedProperties.Add(kvp.Key);
                }
            }

            return affectedProperties;
        }

        /// <summary>
        /// Represent a property value with both the value and the last time it was updated
        /// </summary>
        private struct PropertyValue
        {
            internal PropertyValue(object value, DateTime updated)
            {
                Value = value;
                Updated = updated;
            }

            /// <summary>
            /// Value of the property
            /// </summary>
            public object Value { get; }

            /// <summary>
            /// Last time it was updated
            /// </summary>
            public DateTime Updated { get; }
        }
    }
}
