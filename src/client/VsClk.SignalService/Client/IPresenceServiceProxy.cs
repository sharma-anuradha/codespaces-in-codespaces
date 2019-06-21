using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// EventArgs to notify when properties on a contact changed on the signalR hub
    /// </summary>
    public class UpdatePropertiesEventArgs : EventArgs
    {
        internal UpdatePropertiesEventArgs(ContactReference contact, Dictionary<string, object> properties, string targetConnectionId)
        {
            Contact = contact;
            Properties = properties;
            TargetConnectionId = targetConnectionId;
        }

        public ContactReference Contact { get; }
        public Dictionary<string, object> Properties { get; }
        public string TargetConnectionId { get; }
    }

    /// <summary>
    /// EventArgs to notify when receiving a message from the signalR hub
    /// </summary>
    public class ReceiveMessageEventArgs : EventArgs
    {
        internal ReceiveMessageEventArgs(
            ContactReference targetContact,
            ContactReference fromContact,
            string messageType,
            object body)
        {
            TargetContact = targetContact;
            FromContact = fromContact;
            Type = messageType;
            Body = body;
        }

        public ContactReference TargetContact { get; }
        public ContactReference FromContact { get; }

        public string Type { get; }
        public object Body { get; }
    }

    /// <summary>
    /// Event to notify when a connection is beign added or removed on a contact
    /// </summary>
    public class ConnectionChangedEventArgs : EventArgs
    {
        internal ConnectionChangedEventArgs(ContactReference contact, ConnectionChangeType changeType)
        {
            Contact = contact;
            ChangeType = changeType;
        }

        public ContactReference Contact { get; }
        public ConnectionChangeType ChangeType { get; }
    }

    /// <summary>
    /// The client presence service proxy
    /// </summary>
    public interface IPresenceServiceProxy
    {
        event EventHandler<UpdatePropertiesEventArgs> UpdateProperties;
        event EventHandler<ReceiveMessageEventArgs> MessageReceived;
        event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;

        Task<Dictionary<string, Dictionary<string, PropertyValue>>> GetSelfConnectionsAsync(string contactId, CancellationToken cancellationToken);

        Task<Dictionary<string, object>> RegisterSelfContactAsync(string contactId, Dictionary<string, object> initialProperties, CancellationToken cancellationToken);

        Task PublishPropertiesAsync(Dictionary<string, object> updateProperties, CancellationToken cancellationToken);

        Task SendMessageAsync(ContactReference targetContact, string messageType, object body, CancellationToken cancellationToken);

        Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(ContactReference[] targetContacts, string[] propertyNames, CancellationToken cancellationToken);

        Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(Dictionary<string, object>[] targetContactProperties, string[] propertyNames, bool useStubContact, CancellationToken cancellationToken);

        Task RemoveSubscriptionAsync(ContactReference[] targetContacts, CancellationToken cancellationToken);

        Task UnregisterSelfContactAsync(CancellationToken cancellationToken);

        Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingProperties, CancellationToken cancellationToken);

        Task<Dictionary<string, Dictionary<string, object>>> SearchContactsAsync(Dictionary<string, SearchProperty> searchProperties, int? maxCount, CancellationToken cancellationToken);

    }
}
