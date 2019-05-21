using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// The contact data entity
    /// </summary>
    public class ContactData
    {
        public ContactData(
            string id,
            Dictionary<string, object>  properties,
            Dictionary<string, Dictionary<string, object>> connections)
        {
            Requires.NotNullOrEmpty(id, nameof(id));
            Id = id;
            Properties = Requires.NotNull(properties, nameof(properties));
            Connections = Requires.NotNull(connections, nameof(connections));
        }

        public ContactData(
            string id,
            Dictionary<string, object> properties)
            : this(id, properties, new Dictionary<string, Dictionary<string, object>>())
        {
        }

        /// <summary>
        /// The contact id
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The aggregated properties from all possible connections
        /// </summary>
        public Dictionary<string, object> Properties { get; }

        /// <summary>
        /// Properties set by every live self connection
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> Connections { get; }
    }

    /// <summary>
    /// The message data entity
    /// </summary>
    public class MessageData
    {
        public MessageData(
            ContactReference fromContact,
            ContactReference targetContact,
            string type,
            object body)
        {
            FromContact = fromContact;
            TargetContact = targetContact;
            Type = type;
            Body = body;
        }

        /// <summary>
        /// The contact who want to send the message
        /// </summary>
        public ContactReference FromContact { get; }

        /// <summary>
        /// The target contact to send the message
        /// </summary>
        public ContactReference TargetContact { get; }

        /// <summary>
        /// Type of the message
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Body content of the message
        /// </summary>
        public object Body { get; }
    }


    /// <summary>
    /// Invoked when a remote contact has changed
    /// </summary>
    /// <param name="sourceId">Id of the source who generated</param>
    /// <param name="connectionId">Id of the connection who generate the change</param>
    /// <param name="contactData">The contact data entity that changed</param>
    /// <param name="updateContactType">Type of contact update</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public delegate Task OnContactChangedAsync(
            string sourceId,
            string connectionId,
            ContactData contactData,
            ContactUpdateType updateContactType,
            CancellationToken cancellationToken);

    /// <summary>
    /// Invoked when a message was send from a remote service
    /// </summary>
    /// <param name="sourceId">Id of the source who generate the notification</param>
    /// <param name="messageData">The message data entity</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public delegate Task OnMessageReceivedAsync(
        string sourceId,
        MessageData messageData,
        CancellationToken cancellationToken);

    /// <summary>
    /// Interface to surface a backplane provider
    /// </summary>
    public interface IBackplaneProvider
    {
        OnContactChangedAsync ContactChangedAsync { get;set;}
        OnMessageReceivedAsync MessageReceivedAsync { get; set; }

        /// <summary>
        /// Priority of this provider compared to others
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Return matching contacts
        /// </summary>
        /// <param name="matchProperties">The match properties to look for</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Array of contact data entities that match the criteria</returns>
        Task<ContactData[]> GetContactsAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken);

        /// <summary>
        /// Get the contact properties
        /// </summary>
        /// <param name="contactId">The contact id to query</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The contact data entity if it exists, null otherwise</returns>
        Task<ContactData> GetContactPropertiesAsync(string contactId, CancellationToken cancellationToken);

        /// <summary>
        /// Update the contact properties
        /// </summary>
        /// <param name="sourceId">Id of the source who generated</param>
        /// <param name="connectionId">Id of the connection who generate the change</param>
        /// <param name="contactData">The contact data that changed</param>
        /// <param name="updateContactType">Type of contact update</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateContactAsync(string sourceId, string connectionId, ContactData contactData, ContactUpdateType updateContactType, CancellationToken cancellationToken);

        /// <summary>
        /// Send a message using the backplane provider
        /// </summary>
        /// <param name="sourceId">Id of the source who need to send the message</param>
        /// <param name="messageData">The message data entity</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken);
    }
}
