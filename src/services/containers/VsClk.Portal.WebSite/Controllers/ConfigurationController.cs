using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
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
                { "environment", this.env.EnvironmentName.ToLower()},
            };

            switch (HttpContext.GetPartner())
            {
                case Partners.GitHub:
                    configuration.Add("portForwardingDomainTemplate", appSettings.GitHubPortForwardingDomainTemplate);
                    configuration.Add("enableEnvironmentPortForwarding", appSettings.GitHubportForwardingEnableEnvironmentEndpoints == "true");
                    break;
                case Partners.VSOnline:
                    configuration.Add("portForwardingDomainTemplate", appSettings.PortForwardingDomainTemplate);
                    configuration.Add("enableEnvironmentPortForwarding", appSettings.PortForwardingEnableEnvironmentEndpoints == "true");
                    break;
            }

            return Json(configuration);
        }
    }
}