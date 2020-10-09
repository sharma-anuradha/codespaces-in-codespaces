using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Filters;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Models;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.CodespacesApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class AuthController : Controller
    {
        private AppSettings AppSettings { get; set; }
        private ICookieEncryptionUtils CookieEncryptionUtils { get; }
        private ICodespacesApiClient CodespacesApiClient { get; }
        private ILiveShareTokenExchangeUtil TokenExchangeUtil { get; }

        public AuthController(
            AppSettings appSettings,
            ICookieEncryptionUtils cookieEncryptionUtils,
            ICodespacesApiClient codespacesApiClient,
            ILiveShareTokenExchangeUtil tokenExchangeUtil)
        {
            AppSettings = appSettings;
            CookieEncryptionUtils = cookieEncryptionUtils;
            CodespacesApiClient = codespacesApiClient;
            TokenExchangeUtil = tokenExchangeUtil;
        }

        [HttpOperationalScope("authenticate_port_forwarder")]
        [HttpPost("~/authenticate-port-forwarder")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> AuthenticatePortForwarderAsync(
            [FromForm] string token,
            [FromForm] string cascadeToken
        )
        {
            if (string.IsNullOrWhiteSpace(token) && string.IsNullOrWhiteSpace(cascadeToken))
            {
                return BadRequest();
            }

            if (!string.IsNullOrEmpty(token))
            {
                cascadeToken = await TokenExchangeUtil.ExchangeTokenAsync(token);
            }

            CookieOptions option = CreateCookieOptions();
            option.Expires = DateTime.Now.AddDays(Constants.PortForwarderCookieExpirationDays);

            var cookie = CookieEncryptionUtils.GetEncryptedCookieContent(cascadeToken);
            Response.Cookies.Append(Constants.PFCookieName, cookie, option);

            return Ok();
        }

        [HttpOperationalScope("logout_port_forwarder")]
        [HttpPost("~/logout-port-forwarder")]
        public IActionResult LogoutPortForwarder()
        {
            CookieOptions option = CreateCookieOptions();
            option.Expires = DateTime.Now.AddDays(-100);

            Response.Cookies.Append(Constants.PFCookieName, string.Empty, option);

            return Ok(200);
        }

        [HttpOperationalScope("authenticate_codespace_step_1")]
        [HttpPost("~/authenticate-workspace/{environmentId}")]
        [Routing.HttpPost(
            "~/authenticate-codespace/{environmentId}",
            "auth.apps.dev.codespaces.githubusercontent.com",
            "auth.apps.ppe.codespaces.githubusercontent.com",
            "auth.apps.codespaces.githubusercontent.com")]
        [Routing.AllowReferer("https://github.com")]
        [Consumes("application/x-www-form-urlencoded")]
        [BrandedView]
        public IActionResult AuthenticateWorkspaceAsync(
            [FromRoute] string environmentId,
            [FromForm] string cascadeToken,
            [FromForm] string featureFlags,
            [FromQuery(Name = "port")] int? port)
        {
            if (string.IsNullOrEmpty(cascadeToken))
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(environmentId))
            {
                return BadRequest();
            }

            if (port == default || port < 1 || port > 65535)
            {
                return BadRequest();
            }

            string host = (HttpContext.GetPartner()) switch
            {
                Partners.GitHub => string.Format(AppSettings.GitHubPortForwardingDomainTemplate, $"{environmentId}-{port}"),
                _ => default,
            };

            if (host == default)
            {
                return BadRequest();
            }

            var originalUrl = new Uri(Request.GetEncodedUrl());
            var actionUriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = host,
                Path = $"/authenticate-codespace/{environmentId}",
                Query = originalUrl.Query
            };

            var details = new AuthenticateWorkspaceFormDetails
            {
                Action = actionUriBuilder.Uri.ToString(),
                CascadeToken = cascadeToken,
                FeatureFlags = featureFlags,
            };

            return View(details);
        }

        public class FeatureFlags
        {
            [JsonProperty("portForwardingServiceEnabled")]
            public bool? PortForwardingServiceEnabled { get; set; }
        }

        // TODO: add exception to authentication
        [HttpOperationalScope("authenticate_codespace_step_2")]
        [HttpPost("~/authenticate-codespace/{environmentId}")]
        [Consumes("application/x-www-form-urlencoded")]
        [Routing.AllowReferer(
            // GitHub
            "https://auth.apps.dev.codespaces.githubusercontent.com",
            "https://auth.apps.ppe.codespaces.githubusercontent.com",
            "https://auth.apps.codespaces.githubusercontent.com",
            // VSCS
            "https://online.dev.core.vsengsaas.visualstudio.com",
            "https://online-ppe.core.vsengsaas.visualstudio.com",
            "https://canary.online.visualstudio.com",
            "https://online.visualstudio.com")]
        public async Task<IActionResult> AuthenticateCodespaceAsync(
            [FromRoute] string environmentId,
            [FromForm] string token,
            [FromForm] string cascadeToken,
            [FromForm] string featureFlags,
            [FromQuery(Name = "port")] int? port,
            [FromQuery(Name = "path")] string path,
            [FromQuery(Name = "query")] string query,
            [FromServices] IDiagnosticsLogger logger)
        {
            if (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(cascadeToken))
            {
                return BadRequest();
            }

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(cascadeToken))
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(environmentId))
            {
                return BadRequest();
            }

            if (port == default || port < 1 || port > 65535)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }

            // TODO: Add redirect url? Validate host, environment, port match?

            logger.AddValue("token_type", string.IsNullOrEmpty(token) ? "cascade" : "aad");
            logger.LogInfo("authenticate_environment_token_type");

            // 1. Fetch the environment record.
            // Note: The token we receive, whether it's cascade token or aad token, has to be valid for fetching environments.
            var environment = await CodespacesApiClient
                .WithAuthToken(string.IsNullOrEmpty(token) ? cascadeToken : token)
                .GetCodespaceAsync(environmentId, logger);

            if (environment == default)
            {
                return NotFound();
            }

            if (environment.Connection == default || environment.Connection.ConnectionSessionId == default)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            // 2. If the token is an aad token, exchange it for cascade with liveshare.
            if (!string.IsNullOrEmpty(token))
            {
                cascadeToken = await TokenExchangeUtil.ExchangeTokenAsync(token);
            }

            if (cascadeToken == default)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            string partner;
            if (Request.Headers.TryGetValue(PortForwardingHeaders.OriginalUrl, out var originalUrlValues) &&
                Uri.TryCreate(originalUrlValues.SingleOrDefault(), UriKind.Absolute, out var originalUrl) &&
                GitHubUtils.IsGithubTLD(originalUrl))
            {
                partner = Partners.GitHub;
            }
            else
            {
                partner = HttpContext.GetPartner();
            }

            bool pfsEnabled = partner switch
            {
                Partners.GitHub => AppSettings.GitHubPortForwardingServiceEnabled == "true",
                Partners.VSOnline => AppSettings.PortForwardingServiceEnabled == "true",
                _ => false
            };
            if (!string.IsNullOrEmpty(featureFlags))
            {
                var flags = JsonConvert.DeserializeObject<FeatureFlags>(featureFlags);
                if (flags.PortForwardingServiceEnabled.HasValue)
                {
                    pfsEnabled = flags.PortForwardingServiceEnabled.Value;
                }
            }
            if (pfsEnabled)
            {
                Response.Cookies.Append(Constants.PFSCookieName, Constants.PFSCookieValue);
            }

            string host = partner switch
            {
                Partners.GitHub => string.Format(AppSettings.GitHubPortForwardingDomainTemplate, $"{environmentId}-{port}"),
                _ => string.Format(AppSettings.PortForwardingDomainTemplate, $"{environmentId}-{port}"),
            };
            CookieOptions option = CreateCookieOptions();
            option.Expires = DateTime.Now.AddDays(Constants.PortForwarderCookieExpirationDays);
            var cookie = CookieEncryptionUtils.GetEncryptedCookieContent(
                cascadeToken,
                environmentId,
                environment.Connection.ConnectionSessionId);
            Response.Cookies.Append(Constants.PFCookieName, cookie, option);

            var redirectUriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = host,
                Path = path,
                Query = query
            };

            Response.Cookies.Append(
                Constants.CorrelationCookieName,
                HttpContext.GetCorrelationId(),
                new CookieOptions { Expires = DateTimeOffset.Now.AddSeconds(30) });

            return Redirect(redirectUriBuilder.Uri.ToString());
        }

        private CookieOptions CreateCookieOptions()
        {
            CookieOptions option = new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            };

            return option;
        }
    }
}
