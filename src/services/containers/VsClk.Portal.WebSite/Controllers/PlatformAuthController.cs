using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class PlatformAuthController : Controller
    {
        [HttpGet("~/platform-auth")]
        public ActionResult Index() => View();
    }
}