using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Filters;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class PortForwarderController : Controller
    {
        private static AppSettings AppSettings { get; set; }
        private PortForwardingHostUtils HostUtils { get; }
        private IHostEnvironment HostEnvironment { get; }

        public PortForwarderController(
            AppSettings appSettings,
            PortForwardingHostUtils hostUtils,
            IHostEnvironment hostEnvironment)
        {
            AppSettings = appSettings;
            HostUtils = hostUtils;
            HostEnvironment = hostEnvironment;
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

        [BrandedView]
        public async Task<IActionResult> Index(string path, [FromServices] IDiagnosticsLogger logger)
        {
            var cascadeToken = string.Empty;
            var sessionId = string.Empty;
            if (Request.Headers.TryGetValue(PortForwardingHeaders.Token, out var tokenValues) &&
                Request.Headers.TryGetValue(PortForwardingHeaders.WorkspaceId, out var connectionIdValues) &&
                Request.Headers.TryGetValue(PortForwardingHeaders.Port, out var portStringValues))
            {
                cascadeToken = tokenValues.SingleOrDefault();
                if (string.IsNullOrWhiteSpace(cascadeToken))
                {
                    return ExceptionView(PortForwardingFailure.NotAuthenticated);
                }

                sessionId = connectionIdValues.SingleOrDefault();
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return ExceptionView(PortForwardingFailure.NotAuthenticated);
                }

                var portString = portStringValues.SingleOrDefault();
                if (string.IsNullOrWhiteSpace(portString))
                {
                    return ExceptionView(PortForwardingFailure.NotAuthenticated);
                }
            }

            switch (path)
            {
                case "error":
                    return ExceptionView();
                // Since all paths in the forwarded domain are redirected to this controller, the request for the service 
                // worker will be as well. In that case, serve up the actual service worker.
                case "service-worker.js":
                    return await FetchStaticAsset("service-worker.js", "application/javascript");
                case "service-worker.js.map":
                    return await FetchStaticAsset("service-worker.js.map", "application/octet-stream");
                case "favicon.ico":
                    return await FetchStaticAsset("favicon.ico", "image/x-icon");
                case "site.css":
                    return await FetchStaticAsset("site.css", "text/css");
                case "spinner-dark.svg":
                    return await FetchStaticAsset("spinner-dark.svg", "image/svg+xml");
                case "ms-logo.svg":
                    return await FetchStaticAsset("ms-logo.svg", "image/svg+xml");
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
                return StatusCode(StatusCodes.Status401Unauthorized);
            }

            string sessionId;
            int port;
            try
            {
                (sessionId, port) = GetPortForwardingSessionDetails(logger);
            }
            catch (InvalidOperationException)
            {
                return BadRequest();
            }

            var isUserAllowedToAccessEnvironment = await CheckUserAccessAsync(cascadeToken, sessionId, logger);
            if (!isUserAllowedToAccessEnvironment)
            {
                return StatusCode(StatusCodes.Status401Unauthorized);
            }

            Response.Headers.Add(PortForwardingHeaders.Token, cascadeToken);
            Response.Headers.Add(PortForwardingHeaders.WorkspaceId, sessionId);
            Response.Headers.Add(PortForwardingHeaders.Port, port.ToString());

            return Ok();
        }

        [HttpGet("~/signin")]
        public IActionResult AuthAsync([FromQuery(Name = "rd")] string returnUrl, [FromServices] IDiagnosticsLogger logger)
        {
            var (_, error) = GetAuthToken(logger);

            return ExceptionView(error, returnUrl);
        }

        private ActionResult ExceptionView(PortForwardingFailure failureReason = PortForwardingFailure.Unknown)
        {
            return ExceptionView(failureReason, Request.GetEncodedUrl());
        }

        private ActionResult ExceptionView(PortForwardingFailure failureReason, string redirectUrl)
        {
            var redirectUriQuery = HttpUtility.ParseQueryString(string.Empty);
            redirectUriQuery.Set("redirectUrl", redirectUrl);

            var redirectUriBuilder = new UriBuilder(AppSettings.PortalEndpoint)
            {
                Path = "/login",
                Query = redirectUriQuery.ToString(),
            };

            var details = new PortForwardingErrorDetails
            {
                FailureReason = failureReason,
                RedirectUrl = redirectUriBuilder.Uri.ToString()
            };

            Response.StatusCode = failureReason == PortForwardingFailure.Unknown
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status401Unauthorized;
            return View("exception", details);
        }

        private async Task<ActionResult> FetchStaticAsset(string path, string mediaType)
        {
            // Locally we don't produce the physical file, so we grab it from the portal itself.
            // The portal runs on https://localhost:443 only right now, because of authentication.
            if (AppSettings.IsLocal)
            {
                var client = new HttpClient();
                var stream = await client.GetStreamAsync($"https://localhost:443/{path}");

                return File(stream, mediaType);
            }

            // The static files in test are limited to the ones that don't need to be built.
            if (AppSettings.IsTest)
            {
                var assetPhysicalPath = Path.Combine(HostEnvironment.ContentRootPath,
                    "ClientApp", "public", path);

                return PhysicalFile(assetPhysicalPath, mediaType);
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
            if (!HostUtils.TryGetPortForwardingSessionDetails(Request, out var sessionDetails))
            {
                logger.AddValue(
                    "session_details_source",
                    Request.Headers.ContainsKey(PortForwardingHeaders.WorkspaceId) &&
                    Request.Headers.ContainsKey(PortForwardingHeaders.Port)
                        ? "headers"
                        : "host"
                );
                logger.LogInfo("portforwarding_get_session_details_failed");

                throw new InvalidOperationException("Cannot extract workspace id and port from current request.");
            }

            logger.AddValue("workspace_id", sessionDetails.WorkspaceId);
            logger.AddValue("port", sessionDetails.Port.ToString());
            logger.LogInfo("portforwarding_get_session_details");
            return sessionDetails;
        }

        private async Task<bool> CheckUserAccessAsync(string cascadeToken, string sessionId, IDiagnosticsLogger logger)
        {
            string userId;
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