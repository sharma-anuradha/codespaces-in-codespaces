using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class ConfigurationController : Controller
    {
        private readonly AppSettings appSettings;
        private IWebHostEnvironment env { get; }

        public ConfigurationController(AppSettings appSettings, IWebHostEnvironment env)
        {
            this.appSettings = appSettings;
            this.env = env;
        }

        //[Authorize]
        [HttpGet("/configuration")]
        public ActionResult Index()
        {
            var configuration = new Dictionary<string, object> {
                { "apiEndpoint", appSettings.ApiEndpoint },
                { "environmentRegistrationEndpoint", appSettings.EnvironmentRegistrationEndpoint },
                { "liveShareEndpoint", appSettings.LiveShareEndpoint },
                { "portalEndpoint", appSettings.PortalEndpoint },
                { "liveShareWebExtensionEndpoint", appSettings.LiveShareWebExtensionEndpoint},
                { "richNavWebExtensionEndpoint", appSettings.RichNavWebExtensionEndpoint },
                { "environment", this.env.EnvironmentName.ToLower()},
            };

            var portForwardingServiceEnabled = appSettings.PortForwardingServiceEnabled == "true";
            configuration.Add("portForwardingServiceEnabled", portForwardingServiceEnabled);
            switch (HttpContext.GetPartner())
            {
                case Partners.GitHub:
                    var portForwardingDomainTemplate = appSettings.GitHubPortForwardingDomainTemplate;
                    configuration.Add("portForwardingDomainTemplate", portForwardingDomainTemplate);
                    configuration.Add("enableEnvironmentPortForwarding", appSettings.GitHubportForwardingEnableEnvironmentEndpoints == "true");
                    break;
                case Partners.VSOnline:
                    configuration.Add("portForwardingDomainTemplate", appSettings.PortForwardingDomainTemplate);
                    configuration.Add("enableEnvironmentPortForwarding", appSettings.PortForwardingEnableEnvironmentEndpoints == "true");

                    // Setting PFS cookie for Codespaces only as github still needs more improvement, need to set it for both after issues are fixed.
                    if (portForwardingServiceEnabled)
                    {
                        CookieOptions option = new CookieOptions
                        {
                            Path = "/",
                            Domain = string.Format(appSettings.PortForwardingDomainTemplate, string.Empty),
                            SameSite = SameSiteMode.None,
                        };
                        Response.Cookies.Append(Constants.PFSCookieName, Constants.PFSCookieValue, option);
                    }
                    break;
            }

            return Json(configuration);
        }
    }
}