using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

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
        public ActionResult Index() => Json(new Dictionary<string, string> {
            { "apiEndpoint", appSettings.ApiEndpoint },
            { "environmentRegistrationEndpoint", appSettings.EnvironmentRegistrationEndpoint },
            { "liveShareEndpoint", appSettings.LiveShareEndpoint },
            { "portalEndpoint", appSettings.PortalEndpoint },
            { "liveShareWebExtensionEndpoint", appSettings.LiveShareWebExtensionEndpoint},
            { "environment", this.env.EnvironmentName.ToLower()},
        });
    }
}