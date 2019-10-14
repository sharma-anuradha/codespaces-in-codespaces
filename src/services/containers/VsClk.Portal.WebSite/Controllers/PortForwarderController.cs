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
            if (path == "error")
            {
                return View("exception");
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

            if (devSessionId != null && devSessionId.EndsWith('/'))
            {
                devSessionId = devSessionId.Substring(0, devSessionId.Length - 1);
            }

            string cascadeToken;
            string host = Request.Host.Value;
            ViewBag.HtmlStr = host;
            string sessionId = AppSettings.IsLocal
                ? devSessionId
                : host.Split("-")[0];
            var cookie = Request.Cookies[Constants.PFCookieName];
            if (!string.IsNullOrEmpty(cookie))
            {
                var payload = AuthController.DecryptCookie(cookie);
                if (payload == null)
                {
                    //cookie is expired or there was an error decrypting the cookie. So we will redirect the user to the main page to set new cookie.
                    TempData["exception"] = "cookiePayload";
                    return View("exception");
                }
                cascadeToken = payload.CascadeToken;
            }
            else
            {
                //in this case user probably try to access the portForwarding link directry without signing in, so will redirect to SignIn page and redirect back to PF
                TempData["exception"] = "NotAuthenticated";
                return View("exception");
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

            var ownerId = await WorkSpaceInfo.GetWorkSpaceOwner(cascadeToken, sessionId, AppSettings.LiveShareEndpoint);

            if (ownerId == null || userId == null)
            {
                TempData["exception"] = "nullError";
                return View("exception");
            }

            if (ownerId == userId)
            {
                var cookiePayload = new LiveShareConnectionDetails
                {
                    CascadeToken = cascadeToken,
                    SessionId = sessionId,
                    LiveShareEndPoint = AppSettings.LiveShareEndpoint
                };

                return View(cookiePayload);
            }

            TempData["exception"] = "NotAuthorized";
            return View("exception");
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