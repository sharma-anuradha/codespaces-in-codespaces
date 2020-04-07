using Microsoft.AspNetCore.Mvc;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Filters;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class PlatformAuthController : Controller
    {
        [BrandedView]
        [HttpGet("~/platform-auth")]
        public ActionResult Index() => View();
    }
}