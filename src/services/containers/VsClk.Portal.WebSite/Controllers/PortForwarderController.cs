using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using System.Net.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Web;
using Microsoft.AspNetCore.Http;

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
                return ExceptionView();
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
            if (path == "Site.css")
            {
                return await FetchStaticAsset("site.css", "text/css");
            }
            if (path == "spinner-dark.svg")
            {
                return await FetchStaticAsset("spinner-dark.svg", "image/svg+xml");
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
                    // Cookie is expired or there was an error decrypting the cookie. So we will redirect the user to the main page to set new cookie.
                    return ExceptionView(PortForwardingFailure.InvalidCookiePayload);
                }
                cascadeToken = payload.CascadeToken;
            }
            else
            {
                // In this case user probably try to access the portForwarding link directory without signing in, so will redirect to SignIn page and redirect back to PF
                return ExceptionView(PortForwardingFailure.NotAuthenticated);
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
                return ExceptionView(PortForwardingFailure.InvalidWorkspaceOrOwner);
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

            return ExceptionView(PortForwardingFailure.NotAuthorized);
        }

        private ActionResult ExceptionView(PortForwardingFailure failureReason = PortForwardingFailure.Unknown)
        {
            var redirectUriBuilder = new UriBuilder(AppSettings.PortalEndpoint);
            var redirectUriQuery = HttpUtility.ParseQueryString(string.Empty);

            redirectUriQuery.Set("redirectUrl", UriHelper.BuildAbsolute(Request.Scheme, Request.Host));
            if (Uri.TryCreate(Request.GetEncodedUrl(), UriKind.Absolute, out Uri uri))
            {
                var query = uri.ParseQueryString();
                var path = query.Get("path");
                if (!string.IsNullOrEmpty(path))
                {
                    if (!path.StartsWith("/"))
                    {
                        path = "/" + path;
                    }

                    var pathAndQuery = path.Split("?");
                    if (pathAndQuery.Length == 1)
                    {
                        redirectUriQuery.Set("redirectUrl", UriHelper.BuildAbsolute(Request.Scheme, Request.Host, pathAndQuery[0]));
                    }
                    else if (pathAndQuery.Length == 2)
                    {
                        redirectUriQuery.Set(
                            "redirectUrl",
                            UriHelper.BuildAbsolute(
                                Request.Scheme,
                                Request.Host,
                                path: pathAndQuery[0],
                                query: QueryString.FromUriComponent("?" + pathAndQuery[1])));
                    }
                }
            }

            redirectUriBuilder.Path = "/login";
            redirectUriBuilder.Query = redirectUriQuery.ToString();

            var details = new PortForwardingErrorDetails()
            {
                FailureReason = failureReason,
                RedirectUrl = redirectUriBuilder.Uri.ToString()
            };

            return View("exception", details);
        }

        private async Task<ActionResult> FetchStaticAsset(string path, string mediaType)
        {
            // Locally we don't produce the physical file, so we grab it from the portal itself.
            // The portal runs on https://localhost:443 only right now, because of authentication.
            if (AppSettings.IsLocal)
            {
                HttpClient client = new HttpClient();
                var stream = await client.GetStreamAsync($"https://localhost:443/{path}");

                return File(stream, mediaType);
            }

            var asset = Path.Combine(Directory.GetCurrentDirectory(),
                            "ClientApp", "build", path);

            return PhysicalFile(asset, mediaType);
        }

    }
}