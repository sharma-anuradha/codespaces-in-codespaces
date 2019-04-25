using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Contract for Hub presence client notifications
    /// </summary>
    public interface IPresenceServiceClientHub
    {
        /// <summary>
        /// Invoked when the hub notify on a Contact if some of the subscribed properties have changed
        /// </summary>
        /// <param name="contactId">The contact id who's properties are bieng changed</param>
        /// <param name="notifyProperties">Dictionary of properties being changed</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateValuesAsync(string contactId, Dictionary<string, object> notifyProperties, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when the hub route a message to a particular client
        /// </summary>
        /// <param name="contactId">The recipient contact who will receive the message</param>
        /// <param name="fromContactId">The contact who originally send the message </param>
        /// <param name="type">Type of the message being send</param>
        /// <param name="body">Body of the message</param>
        /// <returns></returns>
        Task ReceiveMessageAsync(string contactId, string fromContactId, string type, JToken body);
    }
}
