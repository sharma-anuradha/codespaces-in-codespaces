using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using System.Net.Http;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class PortForwarderController : Controller
    {
        private static AppSettings AppSettings { get; set; }

        public PortForwarderController(AppSettings appSettings)
        {
            AppSettings = appSettings;
        }

        [HttpGet("~/portforward")]
        public async Task<ActionResult> Index(
            [FromQuery] string path,
            [FromQuery] string devSessionId
        )
        {
            if (path == "error") {
                return View("error");
            }
            
            // Since all paths in the forwarded domain are redirected to this controller, the request for the service 
            // worker will be as well. In that case, serve up the actual service worker.
            if (path == "service-worker.js")
            {
                return await FetchStaticAsset("service-worker.js", "application/javascript");
            }
            if (path == "service-worker.js.map")
            {
                return await FetchStaticAsset("service-worker.js.map", "application/octet-stream");
            }
            if (path == "favicon.ico")
            {
                return await FetchStaticAsset("favicon.ico", "image/x-icon");
            }

            if (devSessionId != null && devSessionId.EndsWith('/')) {
                devSessionId = devSessionId.Substring(0, devSessionId.Length - 1);
            }

            string cascadeToken;
            string host = Request.Host.Value;
            string sessionId = AppSettings.IsLocal 
                ? devSessionId
                : host.Split("-")[0];
            var cookie = Request.Cookies[Constants.PFCookieName];
            if (!string.IsNullOrEmpty(cookie))
            {
                var payload = AuthController.DecryptCookie(cookie);
                if (payload == null)
                {
                    return Content("Failed to find cookie's payload.");
                }
                cascadeToken = payload.CascadeToken;
            }
            else
            {
                //TODO: Redirect to /signIn
                return Content("No Cookie was Found.");
            }

            var userId = string.Empty;
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadToken(cascadeToken) as JwtSecurityToken;
            foreach (Claim claim in token.Claims)
            {
                if (claim.Type == "userId")
                {
                    userId = claim.Value;
                    break;
                }
            }

            var ownerId = await WorkSpaceInfo.GetWorkSpaceOwner(cascadeToken, sessionId);

            if (ownerId == null || userId == null)
            {
                return Content("userId or OwnerId is not provided.");
            }

            if (ownerId == userId)
            {
                var cookiePayload = new LiveShareConnectionDetails
                {
                    CascadeToken = cascadeToken,
                    SessionId = sessionId,
                    LiveShareEndPoint = AppSettings.IsLocal ? Constants.LiveShareLocalEndPoint : Constants.LiveShareEndPoint
                };

                return View(cookiePayload);
            }

            System.Console.WriteLine("user was not authorized for the workspace.");
            //TODO: redirect to 'Not Authorized' page
            return Content("Access Denied.");
        }

        private async Task<ActionResult> FetchStaticAsset(string path, string mediaType)
        {
            // Locally we don't produce the physical file, so we grab it from the portal itself.
            // The portal runs on https://localhost:3000 only right now, because of authentication.
            if (AppSettings.IsLocal)
            {
                HttpClient client = new HttpClient();
                var stream = await client.GetStreamAsync($"https://localhost:3000/{path}");

                return File(stream, mediaType);
            }

            var asset = Path.Combine(Directory.GetCurrentDirectory(),
                            "ClientApp", "build", path);

            return PhysicalFile(asset, mediaType);
        }

    }
}