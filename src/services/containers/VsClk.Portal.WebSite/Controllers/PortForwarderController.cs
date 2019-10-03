using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;

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
        public async Task<ActionResult> Index([FromQuery] string path)
        {
            try
            {
                // Since all paths in the forwarded domain are redirected to this controller, the request for the service 
                // worker will be as well. In that case, serve up the actual service worker.
                if (path == "service-worker.js")
                {
                    string serviceWorkerPath;
                    if (AppSettings.IsLocal)
                    {
                        serviceWorkerPath = Path.Combine(Directory.GetCurrentDirectory(),
                            "ClientApp", "Public", "service-worker.js");
                    }
                    else
                    {
                        serviceWorkerPath = Path.Combine(Directory.GetCurrentDirectory(),
                            "ClientApp", "build", "service-worker.js");
                    }

                    return PhysicalFile(serviceWorkerPath, "application/javascript");
                }

                string cascadeToken;
                string host = Request.Host.Value;
                string sessionId = host.Split("-")[0];
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
            catch (Exception)
            {
                return null;
            }
        }
    }
}