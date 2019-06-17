using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Stub contact to be used when requesting a subscription by matching properties instead of the self contact id
    /// </summary>
    internal class StubContact : ContactBase
    {
        public StubContact(PresenceService service, string contactId, Dictionary<string, object> matchProperties)
            : base(service, contactId)
        {
            Logger.LogDebug($"StubContact -> contactId:{contactId}");
            MatchProperties = matchProperties;
        }

        /// <summary>
        /// The mathing properties being used 
        /// </summary>
        public Dictionary<string, object> MatchProperties { get; }

        /// <summary>
        /// The resolved contact once the 'real' contact is rgistered trough the service or the backplane providers
        /// </summary>
        public Contact ResolvedContact { get; set; }

        /// <summary>
        /// Notify updated properties for this stub contact
        /// </summary>
        /// <param name="selfConnectionId">The self connection who caused the update</param>
        /// <param name="contactDataProvider">The contact data provider</param>
        /// <param name="affectedProperties">Affected properties</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendUpdatePropertiesAsync(
            string selfConnectionId,
            ContactDataProvider contactDataProvider,
            IEnumerable<string> affectedProperties,
            CancellationToken cancellationToken)
        {
            await Task.WhenAll(GetSendUpdateProperties(selfConnectionId, affectedProperties, contactDataProvider, cancellationToken));
        }

    }
}
