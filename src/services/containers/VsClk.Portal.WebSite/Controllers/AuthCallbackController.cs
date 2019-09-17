using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class AuthCallbackController : Controller
    {
        [HttpGet("~/extension-auth-callback")]
        public ActionResult Index() => View();
    }
}