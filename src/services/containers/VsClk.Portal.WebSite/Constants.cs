using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class Constants
    {
        public static string TokenExchangeEndpoint = "https://prod.liveshare.vsengsaas.visualstudio.com/auth/exchange";

        public static string LiveShareEndPoint = "https://prod.liveshare.vsengsaas.visualstudio.com/api/v0.2/workspace/";
        public static int PortForwarderCookieExpirationDays = 2;
        public static string PFCookieName= "vso-pf";

    }
}
