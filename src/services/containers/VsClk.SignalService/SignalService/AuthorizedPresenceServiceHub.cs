using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// A presence service hub class that use the Jwt bear authentication scheme
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthorizedPresenceServiceHub : PresenceServiceHub
    {
        public AuthorizedPresenceServiceHub(PresenceService presenceService, ILogger<PresenceServiceHub> logger)
            : base(presenceService, logger)
        {
        }

        protected override string GetContactIdentity(string contactId)
        {
            // enforce always to use the Claims parameter from the context call
            return Context?.User?.FindFirst("userId")?.Value ?? contactId;
        }
    }
}
