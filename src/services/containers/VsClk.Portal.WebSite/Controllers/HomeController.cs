using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class HomeController : Controller
    {
        [Authorize]
        [HttpGet("~/info")]
        public ActionResult Index() => View();
    }
}