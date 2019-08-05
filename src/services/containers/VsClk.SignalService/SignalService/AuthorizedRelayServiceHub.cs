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
            var user = Context.User;

            // enforce always to use the Claims parameter from the context call
            var userId = user?.FindFirst("userId")?.Value;
            if (properties == null)
            {
                properties = new Dictionary<string, object>();
            }

            properties["userId"] = userId;
            return properties;
        }
    }
}
