using Microsoft.AspNetCore.Mvc;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Filters;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class AuthCallbackController : Controller
    {
        [BrandedView]
        [HttpGet("~/extension-auth-callback")]
        public ActionResult Index() => View();
    }
}