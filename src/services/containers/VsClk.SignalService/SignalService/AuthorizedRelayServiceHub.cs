using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// A relay service hub class that use the Jwt bear authentication scheme
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthorizedRelayServiceHub : RelayServiceHub
    {
        public AuthorizedRelayServiceHub(RelayService relayService, ILogger<RelayServiceHub> logger)
            : base(relayService, logger)
        {
        }

        protected override Dictionary<string, object> GetParticipantProperties(Dictionary<string, object> properties)
        {
            // enforce always to use the Claims parameter from the context call
            var userId = Context?.User?.FindFirst("userId")?.Value;
            if (userId == null)
            {
                return properties;
            }

            if (properties == null)
            {
                properties = new Dictionary<string, object>();
            }

            properties["userId"] = userId;
            return properties;
        }
    }
}
