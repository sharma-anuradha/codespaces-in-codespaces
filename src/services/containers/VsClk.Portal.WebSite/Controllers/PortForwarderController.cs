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
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Models;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class PortForwarderController : Controller
    {
        private static AppSettings AppSettings { get; set; }
        private PortForwardingHostUtils HostUtils { get; }
        public ICookieEncryptionUtils CookieEncryptionUtils { get; }
        private IHostEnvironment HostEnvironment { get; }

        public PortForwarderController(
            AppSettings appSettings,
            PortForwardingHostUtils hostUtils,
            ICookieEncryptionUtils cookieEncryptionUtils,
            IHostEnvironment hostEnvironment)
        {
            AppSettings = appSettings;
            HostUtils = hostUtils;
            CookieEncryptionUtils = cookieEncryptionUtils;
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
            string cascadeToken;
            string sessionId = default;
            string environmentId = default;
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
            else if (AppSettings.IsLocal)
            {
                // When developing locally, there's no ngninx to set the headers.
                (cascadeToken, _) = GetAuthToken(logger);
                try
                {
                    (sessionId, environmentId, _) = GetPortForwardingSessionDetails(logger) switch
                    {
                        EnvironmentSessionDetails details => (details.WorkspaceId, details.EnvironmentId, details.Port),
                        WorkspaceSessionDetails s => (s.WorkspaceId, default, s.Port),
                        _ => (default, default, default),
                    };
                }
                catch
                {
                    // Noop
                }

                if (string.IsNullOrEmpty(cascadeToken) || string.IsNullOrEmpty(sessionId))
                {
                    return SignIn(new Uri(Request.GetEncodedUrl()), logger);
                }
            }
            else
            {
                return ExceptionView(PortForwardingFailure.NotAuthenticated);
            }

            if (Request.Headers.TryGetValue(PortForwardingHeaders.EnvironmentId, out var enironmentIdValues))
            {
                environmentId = enironmentIdValues.SingleOrDefault();
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
                EnvironmentId = environmentId,
                LiveShareEndPoint = AppSettings.LiveShareEndpoint
            };

            return View(cookiePayload);
        }

        [HttpGet("~/auth")]
        public async Task<IActionResult> AuthAsync(
            [FromServices] IWorkspaceInfo workspaceInfo,
            [FromServices] IDiagnosticsLogger logger)
        {
            var (cascadeToken, _) = GetAuthToken(logger);
            if (cascadeToken == default)
            {
                return StatusCode(StatusCodes.Status401Unauthorized);
            }

            string sessionId;
            string environmentId;
            int port;
            try
            {
                (sessionId, environmentId, port) = GetPortForwardingSessionDetails(logger) switch
                {
                    EnvironmentSessionDetails details => (details.WorkspaceId, details.EnvironmentId, details.Port),
                    WorkspaceSessionDetails s => (s.WorkspaceId, default, s.Port),
                    _ => (default, default, default),
                };

                // Neither cookie nor host have the session id, we need to re-authenticate.
                if (string.IsNullOrEmpty(sessionId))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized);
                }
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status401Unauthorized);
            }

            var isUserAllowedToAccessEnvironment = await CheckUserAccessAsync(cascadeToken, sessionId, workspaceInfo, logger);
            if (!isUserAllowedToAccessEnvironment)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            Response.Headers.Add(PortForwardingHeaders.Token, cascadeToken);
            Response.Headers.Add(PortForwardingHeaders.WorkspaceId, sessionId);
            Response.Headers.Add(PortForwardingHeaders.Port, port.ToString());
            if (environmentId != default)
            {
                Response.Headers.Add(PortForwardingHeaders.EnvironmentId, environmentId);
            }

            return Ok();
        }

        [HttpGet("~/signin")]
        public IActionResult SignIn([FromQuery(Name = "rd")] Uri returnUrl,
            [FromServices] IDiagnosticsLogger logger)
        {
            if (!HostUtils.TryGetPortForwardingSessionDetails(returnUrl.Host, out var sessionDetails))
            {
                return BadRequest();
            }

            var isGithub = GitHubUtils.IsGithubTLD(returnUrl);

            if (sessionDetails is WorkspaceSessionDetails)
            {
                if (!isGithub)
                {
                    var (_, error) = GetAuthToken(logger);

                    return ExceptionView(error, returnUrl.ToString());
                }
                else
                {
                    return Redirect("https://github.com/404");
                }
            }

            if (!(sessionDetails is PartialEnvironmentSessionDetails envDetails))
            {
                return BadRequest();
            }

            var redirectUriBuilder = new UriBuilder(GitHubUtils.IsGithubTLD(returnUrl)
                ? "https://github.com/workspaces/auth"
                : $"{AppSettings.PortalEndpoint}/port-forwarding-sign-in"
            );

            // TODO: ensure double / aren't a thing?
            redirectUriBuilder.Path = $"{redirectUriBuilder.Path}/{envDetails.EnvironmentId}";

            var queryBuilder = HttpUtility.ParseQueryString(string.Empty);
            if (returnUrl.AbsolutePath != "/")
            {
                queryBuilder.Add("path", returnUrl.AbsolutePath);
            }
            if (!string.IsNullOrEmpty(returnUrl.Query))
            {
                queryBuilder.Add("query", returnUrl.Query);
            }

            queryBuilder.Add("port", sessionDetails.Port.ToString());

            redirectUriBuilder.Query = queryBuilder.ToString();

            return Redirect(redirectUriBuilder.Uri.ToString());
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

            var response = View("exception", details);
            response.StatusCode = failureReason == PortForwardingFailure.Unknown
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status401Unauthorized;
            return response;
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

        private (string Token, PortForwardingFailure FailureReason) GetAuthToken(IDiagnosticsLogger logger)
        {
            if (Request.Headers.TryGetValue(PortForwardingHeaders.Authentication, out var tokenValues))
            {
                logger.AddValue("token_source", "header");
                logger.LogInfo("portforwarding_get_token");
                return (tokenValues.SingleOrDefault(), PortForwardingFailure.None);
            }

            if (!TryGetCookiePayload(logger, out var cookiePayload, out var reason))
            {
                return (default, reason);
            }

            return (cookiePayload.CascadeToken, PortForwardingFailure.None);
        }

        private bool TryGetCookiePayload(
            IDiagnosticsLogger logger,
            out PortForwardingAuthCookiePayload cookiePayload)
        {
            return TryGetCookiePayload(logger, out cookiePayload, out var _);
        }

        private bool TryGetCookiePayload(
            IDiagnosticsLogger logger,
            out PortForwardingAuthCookiePayload cookiePayload,
            out PortForwardingFailure reason)
        {
            var cookie = Request.Cookies[Constants.PFCookieName];
            if (string.IsNullOrEmpty(cookie))
            {
                logger.AddValue("failure_reason", "not_authenticated");
                logger.LogInfo("portforwarding_try_get_cookie_payload_failed");

                // In this case user probably try to access the portForwarding link directory without signing in, so will redirect to SignIn page and redirect back to PF
                cookiePayload = default;
                reason = PortForwardingFailure.NotAuthenticated;
                return false;
            }

            logger.AddValue("token_source", "cookie");
            cookiePayload = CookieEncryptionUtils.DecryptCookie(cookie);
            if (cookiePayload == default)
            {
                logger.AddValue("failure_reason", "invalid_cookie_payload");
                logger.LogInfo("portforwarding_try_get_cookie_payload_failed");

                // Cookie is expired or there was an error decrypting the cookie.
                cookiePayload = default;
                reason = PortForwardingFailure.InvalidCookiePayload;
                return false;
            }

            logger.AddValue("environment_id", cookiePayload.EnvironmentId);
            logger.AddValue("connection_session_id", cookiePayload.ConnectionSessionId);
            logger.LogInfo("portforwarding_try_get_cookie_payload");

            reason = PortForwardingFailure.None;
            return true;
        }

        private PortForwardingSessionDetails GetPortForwardingSessionDetails(IDiagnosticsLogger logger)
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
                logger.AddValue("reason", "invalid_session_details");
                logger.LogInfo("portforwarding_get_session_details_failed");

                throw new InvalidOperationException("Cannot extract workspace id and port from current request.");
            }

            // TODO: Add support for environment id connection through headers?
            if (sessionDetails is PartialEnvironmentSessionDetails envDetails)
            {
                if (!TryGetCookiePayload(logger, out var cookiePayload))
                {
                    logger.AddValue("reason", "no_cookie_payload");
                    logger.LogInfo("portforwarding_get_session_details_failed");

                    throw new InvalidOperationException("No cookie set for environment authentication.");
                }
                else if (!string.Equals(cookiePayload.EnvironmentId, envDetails.EnvironmentId, StringComparison.InvariantCultureIgnoreCase))
                {
                    logger.AddValue("reason", "cookie_environment_id_match");
                    logger.LogInfo("portforwarding_get_session_details_failed");

                    throw new InvalidOperationException("No cookie set for environment authentication.");
                }

                // Since we don't get the WorkspaceId from the host, we'll take it from the cookie.
                sessionDetails = new EnvironmentSessionDetails(cookiePayload.ConnectionSessionId, envDetails.EnvironmentId, envDetails.Port);
            }

            if (sessionDetails is WorkspaceSessionDetails wsDetails)
            {
                logger.AddValue("workspace_id", wsDetails.WorkspaceId);
            }

            logger.AddValue("port", sessionDetails.Port.ToString());

            if (sessionDetails is EnvironmentSessionDetails details)
            {
                logger.AddValue("environment_id", details.EnvironmentId);
            }

            logger.LogInfo("portforwarding_get_session_details");

            return sessionDetails;
        }

        private async Task<bool> CheckUserAccessAsync(string cascadeToken, string sessionId, IWorkspaceInfo workspaceInfo, IDiagnosticsLogger logger)
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
            var ownerId = await workspaceInfo.GetWorkSpaceOwner(cascadeToken, sessionId, AppSettings.LiveShareEndpoint);
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