using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.ControllerAccess;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class PortForwarderController : Controller
    {
        private static AppSettings AppSettings { get; set; }

        private readonly IControllerProvider controllerProvider;

        public PortForwarderController(
            AppSettings appSettings,
            IControllerProvider controllerProvider)
        {
            AppSettings = appSettings;
            this.controllerProvider = controllerProvider;
        }

        private string GetTokenClaim(string claimName, JwtSecurityToken token)
        {
            foreach (Claim claim in token.Claims)
            {
                if (claim.Type == claimName)
                {
                    return claim.Value;
                }
            }

            return string.Empty;
        }

        [HttpGet("~/portforward")]
        public async Task<IActionResult> Index(
            [FromQuery] string path,
            [FromServices] IDiagnosticsLogger logger
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
            if (path == "site.css")
            {
                return await FetchStaticAsset("site.css", "text/css");
            }
            if (path == "spinner-dark.svg")
            {
                return await FetchStaticAsset("spinner-dark.svg", "image/svg+xml");
            }
            if (path == "ms-logo.svg")
            {
                return await FetchStaticAsset("ms-logo.svg", "image/svg+xml");
            }

            var (cascadeToken, error) = GetAuthToken(logger);
            if (cascadeToken == default)
            {
                return ExceptionView(error);
            }

            string sessionId;
            try
            {
                (sessionId, _) = GetPortForwardingSessionDetails(logger);
            }
            catch (InvalidOperationException)
            {
                return BadRequest();
            }

            var isUserAllowedToAccessEnvironment = await CheckUserAccessAsync(cascadeToken, sessionId, logger);
            if (!isUserAllowedToAccessEnvironment)
            {
                return ExceptionView(PortForwardingFailure.NotAuthorized);
            }

            var cookiePayload = new LiveShareConnectionDetails
            {
                CascadeToken = cascadeToken,
                SessionId = sessionId,
                LiveShareEndPoint = AppSettings.LiveShareEndpoint
            };

            return View(cookiePayload);
        }

        [HttpGet("~/auth")]
        public async Task<IActionResult> AuthAsync([FromServices] IDiagnosticsLogger logger)
        {
            var (cascadeToken, error) = GetAuthToken(logger);
            if (cascadeToken == default)
            {
                return ExceptionView(error);
            }

            string sessionId;
            try
            {
                (sessionId, _) = GetPortForwardingSessionDetails(logger);
            }
            catch (InvalidOperationException)
            {
                return BadRequest();
            }

            var isUserAllowedToAccessEnvironment = await CheckUserAccessAsync(cascadeToken, sessionId, logger);
            if (!isUserAllowedToAccessEnvironment)
            {
                return ExceptionView(PortForwardingFailure.NotAuthorized);
            }

            Response.Headers.Add("X-VSOnline-Forwarding-Token", cascadeToken);
            return Ok();
        }

        [HttpPost("~/portforward")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> PostAsync(
            [FromQuery] string path
        )
        {
            // Add this header in case there is any confusion about which service the reponse is coming from
            Response.Headers.Add("X-Powered-By", "Visual Studio Online Portal");

            if (path == "authenticate-port-forwarder")
            {
                this.Request.Form.TryGetValue("token", out var tokenValues);
                this.Request.Form.TryGetValue("cascadeToken", out var cascadeTokenValues);

                var token = tokenValues.SingleOrDefault();
                var cascadeToken = cascadeTokenValues.SingleOrDefault();

                var authController = controllerProvider.Create<AuthController>(this.ControllerContext);
                return await authController.AuthenticatePortForwarderAsync(token, cascadeToken);
            }
            if (path == "logout-port-forwarder")
            {
                var authController = controllerProvider.Create<AuthController>(this.ControllerContext);
                return authController.LogoutPortForwarder();
            }

            // This most likely should have gone to the service worker instead
            return BadRequest();
        }

        private ActionResult ExceptionView(PortForwardingFailure failureReason = PortForwardingFailure.Unknown)
        {
            var redirectUriBuilder = new UriBuilder(AppSettings.PortalEndpoint);
            var redirectUriQuery = HttpUtility.ParseQueryString(string.Empty);

            redirectUriQuery.Set("redirectUrl", UriHelper.BuildAbsolute(Request.Scheme, Request.Host));
            if (Uri.TryCreate(Request.GetEncodedUrl(), UriKind.Absolute, out Uri uri))
            {
                var query = HttpUtility.ParseQueryString(uri.Query);
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

            Response.StatusCode = StatusCodes.Status401Unauthorized;
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

        private (string, PortForwardingFailure) GetAuthToken(IDiagnosticsLogger logger)
        {
            if (Request.Headers.TryGetValue("X-VSOnline-Authentication", out var tokenValues))
            {
                logger.AddValue("token_source", "header");
                logger.LogInfo("portforwarding_get_token");
                return (tokenValues.FirstOrDefault(), PortForwardingFailure.None);
            }

            var cookie = Request.Cookies[Constants.PFCookieName];
            if (string.IsNullOrEmpty(cookie))
            {
                logger.AddValue("failure_reason", "not_authenticated");
                logger.LogInfo("portforwarding_get_token_failed");
                // In this case user probably try to access the portForwarding link directory without signing in, so will redirect to SignIn page and redirect back to PF
                return (null, PortForwardingFailure.NotAuthenticated);
            }

            logger.AddValue("token_source", "cookie");
            var payload = AuthController.DecryptCookie(cookie, AppSettings.AesKey);
            if (payload == null)
            {
                logger.AddValue("failure_reason", "invalid_cookie_payload");
                logger.LogInfo("portforwarding_get_token_failed");
                // Cookie is expired or there was an error decrypting the cookie.
                return (null, PortForwardingFailure.InvalidCookiePayload);
            }

            logger.LogInfo("portforwarding_get_token");
            return (payload.CascadeToken, PortForwardingFailure.None);
        }

        private (string SessionId, int Port) GetPortForwardingSessionDetails(IDiagnosticsLogger logger)
        {
            string workspaceId;
            string portString;
            if (!Request.Headers.TryGetValue("X-VSOnline-Forwarding-WorkspaceId", out var workspaceIdValues) ||
                !Request.Headers.TryGetValue("X-VSOnline-Forwarding-Port", out var portStringValues))
            {
                logger.AddValue("session_details_source", "host");

                var hostString = Request.Host.ToString();

                var match = Regex.Match(hostString, "(?<workspaceId>[0-9A-Fa-f]{36})-(?<port>\\d{2,5})");
                workspaceId = match.Groups["workspaceId"].Value;
                portString = match.Groups["port"].Value;
            }
            else
            {
                logger.AddValue("session_details_source", "headers");

                workspaceId = workspaceIdValues.FirstOrDefault();
                portString = portStringValues.FirstOrDefault();
            }

            if (!Regex.IsMatch(workspaceId, "^[0-9A-Fa-f]{36}$") || !int.TryParse(portString, out int port))
            {
                logger.LogInfo("portforwarding_get_session_details_failed");
                throw new InvalidOperationException("Cannot extract workspace id and port from current request.");
            }

            logger.AddValue("workspace_id", workspaceId);
            logger.AddValue("port", port.ToString());
            logger.LogInfo("portforwarding_get_session_details");
            return (workspaceId, port);
        }

        private async Task<bool> CheckUserAccessAsync(string cascadeToken, string sessionId, IDiagnosticsLogger logger)
        {
            var userId = string.Empty;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadToken(cascadeToken) as JwtSecurityToken;
                userId = GetTokenClaim("userId", token);

                // For the VSO tokens generated for partners, there is no `userId` nor `subject` claims 
                // so calculate the user id based on the `tid`/`oid` claims instead.
                if (string.IsNullOrEmpty(userId))
                {
                    var tid = GetTokenClaim("tid", token);
                    var oid = GetTokenClaim("oid", token);

                    if (!string.IsNullOrEmpty(tid) && !string.IsNullOrEmpty(oid))
                    {
                        userId = $"{tid}_{oid}";
                    }
                }
            }
            catch (ArgumentException)
            {
                logger.AddValue("failure_reason", "unable_to_read_token");
                logger.LogWarning("portforwarding_check_user_access_failed");
                return false;
            }

            if (string.IsNullOrEmpty(userId))
            {
                logger.AddValue("failure_reason", "no_user_claim");
                logger.LogWarning("portforwarding_check_user_access_failed");
                return false;
            }

            logger.AddValue("workspace_id", sessionId);
            var lsWorkspaceQueryDuration = logger.StartDuration();
            var ownerId = await WorkSpaceInfo.GetWorkSpaceOwner(cascadeToken, sessionId, AppSettings.LiveShareEndpoint);
            logger.AddDuration(lsWorkspaceQueryDuration);
            logger.LogInfo("portforwarding_check_user_access_query_workspace");

            if (string.IsNullOrEmpty(ownerId))
            {
                logger.AddValue("failure_reason", "no_workspace_owner");
                logger.LogWarning("portforwarding_check_user_access_failed");
                return false;
            }

            if (ownerId != userId)
            {
                logger.AddValue("failure_reason", "user_owner_dont_match");
                logger.LogWarning("portforwarding_check_user_access_failed");
                return false;
            }

            logger.LogInfo("portforwarding_check_user_access");
            return true;
        }
    }
}