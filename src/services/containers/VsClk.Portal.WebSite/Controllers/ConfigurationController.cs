using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class ConfigurationController : Controller
    {
        private readonly AppSettings appSettings;

        public ConfigurationController(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        //[Authorize]
        [HttpGet("/configuration")]
        public ActionResult Index() => Json(new Dictionary<string, string> {
            { "apiEndpoint", appSettings.ApiEndpoint },
            { "environmentRegistrationEndpoint", appSettings.EnvironmentRegistrationEndpoint },
            { "liveShareEndpoint", appSettings.LiveShareEndpoint },
            { "portalEndpoint", appSettings.PortalEndpoint }
        });
    }
}