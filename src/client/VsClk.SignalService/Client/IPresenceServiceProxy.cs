using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// EventArgs to notify when properties on a contact changed on the signalR hub
    /// </summary>
    public class UpdatePropertiesEventArgs : EventArgs
    {
        internal UpdatePropertiesEventArgs(string contactdId, Dictionary<string, object> properties)
        {
            ContactId = contactdId;
            Properties = properties;
        }

        public string ContactId { get; }
        public Dictionary<string, object> Properties { get; }
    }

    /// <summary>
    /// EventArgs to notif when receiving a message form the signalR hub
    /// </summary>
    public class ReceiveMessageEventArgs : EventArgs
    {
        internal ReceiveMessageEventArgs(
            string contactdId,
            string fromContactId,
            string messageType,
            JToken body)
        {
            ContactId = contactdId;
            FromContactId = fromContactId;
            Type = messageType;
            Body = body;
        }

        public string ContactId { get; }
        public string FromContactId { get; }

        public string Type { get; }
        public JToken Body { get; }
    }

    /// <summary>
    /// The client presence service proxy
    /// </summary>
    public interface IPresenceServiceProxy
    {
        event EventHandler<UpdatePropertiesEventArgs> UpdateProperties;
        event EventHandler<ReceiveMessageEventArgs> MessageReceived;

        Task RegisterSelfContactAsync(string contactId, Dictionary<string, object> initialProperties, CancellationToken cancellationToken);

        Task PublishPropertiesAsync(Dictionary<string, object> updateProperties, CancellationToken cancellationToken);

        Task SendMessageAsync(string targetContactId, string messageType, JToken body, CancellationToken cancellationToken);

        Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(string[] targetContactIds, string[] propertyNames, CancellationToken cancellationToken);

        Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(Dictionary<string, object>[] targetContactProperties, string[] propertyNames, bool useStubContact, CancellationToken cancellationToken);

        Task RemoveSubcriptionPropertiesAsync(string[] targetContactIds, string[] propertyNames, CancellationToken cancellationToken);

        Task RemoveSubscriptionAsync(string[] targetContactIds, CancellationToken cancellationToken);

        Task UnregisterSelfContactAsync(CancellationToken cancellationToken);

        Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingProperties, CancellationToken cancellationToken);

        Task<Dictionary<string, Dictionary<string, object>>> SearchContactsAsync(Dictionary<string, SearchProperty> searchProperties, int? maxCount, CancellationToken cancellationToken);

    }
}
