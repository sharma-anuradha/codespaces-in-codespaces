using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Type of change when updating a contact
    /// </summary>
    public enum ContactUpdateType
    {
        /// <summary>
        /// Default none option
        /// </summary>
        None,

        /// <summary>
        /// When a contact is being registered
        /// </summary>
        Registration,

        /// <summary>
        /// When the contact is being updated
        /// </summary>
        UpdateProperties,
    }

    /// <summary>
    /// Invoked when a remote contact has changed
    /// </summary>
    /// <param name="sourceId">Id of the source who generated</param>
    /// <param name="contactId">The contact id of the contact that has changed</param>
    /// <param name="properties">Updated properties</param>
    /// <param name="updateContactType">Type of contact update</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public delegate Task OnContactChangedAsync(
            string sourceId,
            string contactId,
            Dictionary<string, object> properties,
            ContactUpdateType updateContactType,
            CancellationToken cancellationToken);

    /// <summary>
    /// Invoked when a message was send from a remote service
    /// </summary>
    /// <param name="sourceId">Id of the source who generated</param>
    /// <param name="contactId">The contact id that send the message</param>
    /// <param name="targetContactId">The target contact id to send the message</param>
    /// <param name="type">Type of messagr to send</param>
    /// <param name="body">Body of the message</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public delegate Task OnMessageReceivedAsync(
        string sourceId,
        string contactId,
        string targetContactId,
        string type,
        JToken body,
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
        /// <returns>Array of contact properties that match the criteria</returns>
        Task<Dictionary<string, object>[]> GetContactsAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken);

        /// <summary>
        /// Get the contact properties
        /// </summary>
        /// <param name="contactId">The contact id to query</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The properties of the contact or null if it is not available</returns>
        Task<Dictionary<string, object>> GetContactPropertiesAsync(string contactId, CancellationToken cancellationToken);

        /// <summary>
        /// Update the contact properties
        /// </summary>
        /// <param name="sourceId">Id of the source who generated</param>
        /// <param name="contactId">The contact id to update</param>
        /// <param name="properties">New properties</param>
        /// <param name="updateContactType">Type of contact update</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateContactAsync(string sourceId, string contactId, Dictionary<string, object> properties, ContactUpdateType updateContactType, CancellationToken cancellationToken);

        /// <summary>
        /// Send a message using the backplane provider
        /// </summary>
        /// <param name="sourceId">Id of the source who generated</param>
        /// <param name="contactId">The originator contact id</param>
        /// <param name="targetContactId">The target contact id</param>
        /// <param name="messageType">Message type</param>
        /// <param name="body">Body of the message</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendMessageAsync(string sourceId, string contactId, string targetContactId, string messageType, JToken body, CancellationToken cancellationToken);
    }
}
