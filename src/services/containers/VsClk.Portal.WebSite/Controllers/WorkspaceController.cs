using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class WorkspaceController : Controller
    {
        [HttpGet("~/workspace/{id}")]
        public ActionResult Index() => View();
    }
}