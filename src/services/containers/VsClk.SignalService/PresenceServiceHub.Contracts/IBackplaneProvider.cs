using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Class to describe a contact change
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ContactDataChanged<T>
    {
        public ContactDataChanged(string serviceId, string connectionId, string contactId, ContactUpdateType updateContactType, T data)
        {
            Requires.NotNullOrEmpty(serviceId, nameof(serviceId));
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(contactId, nameof(contactId));

            ServiceId = serviceId;
            ConnectionId = connectionId;
            ContactId = contactId;
            Type = updateContactType;
            Data = data;
        }

        public string ServiceId { get; }
        public string ConnectionId { get; }
        public string ContactId { get; }
        public ContactUpdateType Type { get; }
        public T Data { get; }
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
    /// <param name="contactDataChanged">The contact data info that changed</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public delegate Task OnContactChangedAsync(
            ContactDataChanged<ContactDataInfo> contactDataChanged,
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
        Task<Dictionary<string, ContactDataInfo>> GetContactsDataAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken);

        /// <summary>
        /// Get the contact data info
        /// </summary>
        /// <param name="contactId">The contact id to query</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The contact data entity if it exists, null otherwise</returns>
        Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken);

        /// <summary>
        /// Update the contact properties
        /// </summary>
        /// <param name="contactDataChanged">The contact data info that changed</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken);

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
