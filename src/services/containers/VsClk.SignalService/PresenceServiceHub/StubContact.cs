using System.Collections.Generic;
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
    }
}
