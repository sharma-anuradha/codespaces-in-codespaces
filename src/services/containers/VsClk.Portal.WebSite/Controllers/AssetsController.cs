using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class AssetsController : Controller
    {
        [HttpGet("/assets/{sessionId}/{port}/vscode-remote-resource")]
        public ActionResult VsCodeAsset() => NotFound();
    }
}
