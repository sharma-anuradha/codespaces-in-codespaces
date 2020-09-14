using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class ConfigurationController : Controller
    {
        private AppSettings AppSettings { get; }
        private IWebHostEnvironment WebHostEnvironment { get; }

        public ConfigurationController(AppSettings appSettings, IWebHostEnvironment env)
        {
            AppSettings = appSettings;
            WebHostEnvironment = env;
        }

        //[Authorize]
        [HttpGet("/configuration")]
        public ActionResult Index()
        {
            var configuration = new Dictionary<string, object> {
                { "isDevStamp", AppSettings.IsDevStamp },
                { "apiEndpoint", AppSettings.ApiEndpoint },
                { "environmentRegistrationEndpoint", AppSettings.EnvironmentRegistrationEndpoint },
                { "liveShareEndpoint", AppSettings.LiveShareEndpoint },
                { "portalEndpoint", AppSettings.PortalEndpoint },
                { "liveShareWebExtensionEndpoint", AppSettings.LiveShareWebExtensionEndpoint},
                { "richNavWebExtensionEndpoint", AppSettings.RichNavWebExtensionEndpoint },
                { "environment", this.WebHostEnvironment.EnvironmentName.ToLower()},
            };

            switch (HttpContext.GetPartner())
            {
                case Partners.GitHub:
                    configuration.Add("portForwardingServiceEnabled", AppSettings.GitHubPortForwardingServiceEnabled == "true");
                    configuration.Add("portForwardingDomainTemplate", AppSettings.GitHubPortForwardingDomainTemplate);
                    configuration.Add("portForwardingManagementEndpoint", AppSettings.GitHubPortForwardingManagementEndpoint);
                    configuration.Add("enableEnvironmentPortForwarding", AppSettings.GitHubportForwardingEnableEnvironmentEndpoints == "true");
                    break;
                case Partners.VSOnline:
                    configuration.Add("portForwardingServiceEnabled", AppSettings.PortForwardingServiceEnabled == "true");
                    configuration.Add("portForwardingDomainTemplate", AppSettings.PortForwardingDomainTemplate);
                    configuration.Add("portForwardingManagementEndpoint", AppSettings.PortForwardingManagementEndpoint);
                    configuration.Add("enableEnvironmentPortForwarding", AppSettings.PortForwardingEnableEnvironmentEndpoints == "true");
                    break;
            }

            return Json(configuration);
        }
    }
}