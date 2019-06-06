using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Service exposed by the presence hub
    /// </summary>
    public interface IPresenceServiceHub
    {
        /// <summary>
        /// Get all the self connections associated with a contact id
        /// </summary>
        /// <param name="contactId"></param>
        /// <returns></returns>
        Task<Dictionary<string, Dictionary<string, object>>> GetSelfConnectionsAsync(string contactId);

        /// <summary>
        /// Register a self contact for later subscription
        /// </summary>
        /// <param name="contactId">Unique contact identifier</param>
        /// <param name="initialProperties">The  optional initial properties to populate</param>
        /// <returns></returns>
        Task<Dictionary<string, object>> RegisterSelfContactAsync(string contactId, Dictionary<string, object> initialProperties);

        /// <summary>
        /// Publish modified properties into the registered contact
        /// </summary>
        /// <param name="updateProperties">New updated values to publish</param>
        /// <returns></returns>
        Task PublishPropertiesAsync(Dictionary<string, object> updateProperties);

        /// <summary>
        /// Add multiple subscriptions on some registered contacts
        /// </summary>
        /// <param name="targetContactIds">List of target contacts where to start a subscription</param>
        /// <param name="propertyNames">Name of the properties to subscribe</param>
        /// <returns></returns>
        Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(ContactReference[] targetContacts, string[] propertyNames);

        /// <summary>
        /// Request multiple subscriptions based on matching properties
        /// </summary>
        /// <param name="targetContactProperties">The target contacts defined by multiple matching properties</param>
        /// <param name="propertyNames">Name of the properties to subscribe</param>
        /// <param name="useStubContact">If usign a stub contact is enabled</param>
        /// <returns>An array with each item of existing targeted property values or null if it wasn't any match</returns>
        Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(Dictionary<string, object>[] targetContactProperties, string[] propertyNames, bool useStubContact);

        /// <summary>
        /// Remove a subscription
        /// </summary>
        /// <param name="targetContactIds">List of target contacts where to remove a subscription</param>
        void RemoveSubscription(ContactReference[] targetContacts);

        /// <summary>
        /// Send a message to registered contact
        /// </summary>
        /// <param name="targetContact">The target contact to send the message</param>
        /// <param name="messageType">The type of message</param>
        /// <param name="body">Body of the message</param>
        /// <returns></returns>
        /// 
        Task SendMessageAsync(ContactReference targetContact, string messageType, object body);

        /// <summary>
        /// Remove a registration on this hub
        /// </summary>
        /// <returns></returns>
        Task UnregisterSelfContactAsync();

        /// <summary>
        /// Multiple matching of registered contacts
        /// </summary>
        /// <param name="matchingProperties">Lists of matching properties</param>
        /// <returns></returns>
        Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingProperties);


        /// <summary>
        /// Search on all the published contacts
        /// </summary>
        /// <param name="searchProperties"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        Task<Dictionary<string, Dictionary<string, object>>> SearchContactsAsync(Dictionary<string, SearchProperty> searchProperties, int? maxCount);
    }

    /// <summary>
    /// The property search entity
    /// </summary>
    public class SearchProperty
    {
        /// <summary>
        /// Represent a regular expression to apply into a property
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Options to the regular expression
        /// </summary>
        public int? Options { get; set; }
    }
}
