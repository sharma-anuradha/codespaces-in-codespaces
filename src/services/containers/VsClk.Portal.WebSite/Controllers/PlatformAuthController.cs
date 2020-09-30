using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Authentication;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class PlatformAuthController : Controller
    {
        private static AppSettings AppSettings { get; set; }
        private IHostEnvironment HostEnvironment { get; }

        public PlatformAuthController(AppSettings appSettings, IHostEnvironment hostEnvironment)
        {
            AppSettings = appSettings;
            HostEnvironment = hostEnvironment;
        }

        [BrandedView]
        [HttpGet("~/platform-auth")]
        public Task<ActionResult> Index() => FetchStaticAsset("platform-auth.html", "text/html");

        [HttpPost("~/platform-auth")]
        [Authorize(AuthenticationSchemes = AuthenticationServiceCollectionExtensions.VsoBodyAuthenticationScheme)]
        public async Task<ActionResult> PlatformAuth()
        {
            var cookieScheme = AuthenticationServiceCollectionExtensions.CookieNoSameSiteScheme;
            var claimsIdentity = new ClaimsIdentity(
                HttpContext.User.Claims, cookieScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2),
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                cookieScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return await FetchStaticAsset("platform-auth.html", "text/html");
        }

        private async Task<ActionResult> FetchStaticAsset(string path, string mediaType)
        {
            // Locally we don't produce the physical file, so we grab it from the portal itself.
            // The portal runs on https://localhost:443 only right now, because of authentication.
            if (AppSettings.IsLocal)
            {
                var client = new HttpClient();
                var stream = await client.GetStreamAsync($"http://localhost:3030/{path}");

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

        public class VSCodeSettings
        {
            [JsonProperty("homeIndicator")]
            public object HomeIndicator { get; set; }

            [JsonProperty("defaultSettings")]
            public Dictionary<string, object> DefaultSettings { get; set; }

            [JsonProperty("defaultExtensions")]
            public List<object> DefaultExtensions { get; set; }

            [JsonProperty("enableSyncByDefault")]
            public bool EnableSyncByDefault { get; set; }

            [JsonProperty("authenticationSessionId")]
            public string AuthenticationSessionId { get; set; }

            [JsonProperty("defaultAuthSessions")]
            public List<object> DefaultAuthSessions { get; set; }

            [JsonProperty("vscodeChannel")]
            public string VSCodeChannel { get; set; }

            [JsonProperty("loadingScreenThemeColor")]
            public string LoadingScreenThemeColor { get; set; }
        }

        public class Favicon
        {
            [JsonProperty("stable")]
            public string Stable { get; set; }

            [JsonProperty("insider")]
            public string Insider { get; set; }
        }

        public class PartnerInfo
        {
            [JsonProperty("partnerName")]
            public string PartnerName { get; set; }

            [JsonProperty("managementPortalUrl")]
            public string ManagementPortalUrl { get; set; }

            [JsonProperty("codespaceId")]
            public string CodespaceId { get; set; }

            [JsonProperty("cascadeToken")]
            public string CascadeToken { get; set; }

            [JsonProperty("codespaceToken")]
            public string CodespaceToken { get; set; }

            [JsonProperty("vscodeSettings")]
            public VSCodeSettings VSCodeSettings { get; set; }

            [JsonProperty("featureFlags")]
            public object FeatureFlags { get; set; }

            [JsonProperty("favicon")]
            public Favicon Favicon { get; set; }

            [JsonProperty("credentials")]
            public List<object> Credentials { get; set; }
        }

        [RestrictIFrame]
        [HttpPost("~/")]
        [HttpPost("~/connect")]
        [HttpPost("~/platform-authentication")]
        [Authorize(AuthenticationSchemes = AuthenticationServiceCollectionExtensions.VsoBodyAuthenticationScheme)]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult PlatformAuthentication(
            [FromForm] string partnerInfo,
            [FromForm] string codespaceToken,
            [FromForm] string cascadeToken
        )
        {
            if (string.IsNullOrWhiteSpace(partnerInfo))
            {
                return BadRequest("No `partnerInfo` set in the body.");
            }

            var partnerInfoData = JsonConvert.DeserializeObject<PartnerInfo>(partnerInfo);
            var formCodespacesToken = cascadeToken ?? codespaceToken;

            // TODO: validate against JSON schema instead
            if (formCodespacesToken != partnerInfoData.CodespaceToken)
            {
                return BadRequest("Codespaces token in request body and in the partner info payload do not match.");
            }

            if (string.IsNullOrWhiteSpace(partnerInfoData.ManagementPortalUrl))
            {
                return BadRequest("No `managementPortalUrl` set.");
            }

            if (string.IsNullOrWhiteSpace(partnerInfoData.PartnerName))
            {
                return BadRequest("No `partnerName` set.");
            }

            if (string.IsNullOrWhiteSpace(partnerInfoData.CodespaceId))
            {
                return BadRequest("No `codespaceId` set.");
            }

            var json = JsonConvert.SerializeObject(partnerInfoData);
            byte[] data = System.Text.ASCIIEncoding.ASCII.GetBytes(json);

            ViewData["partner-info"] = System.Convert.ToBase64String(data);
            ViewData["is-local"] = AppSettings.IsLocal;

            // To make the loading screen transition seamless with our partners,
            // we need to prerender the shimmer early to prevent any flickering
            // the classnames below used to define the shimmer appearence class names
            var themeColor = partnerInfoData.VSCodeSettings.LoadingScreenThemeColor ?? "light";
            var paramsTheme = HttpContext.Request.Query["loadingScreenThemeColor"];

            if (!string.IsNullOrWhiteSpace(paramsTheme))
            {
                switch (paramsTheme)
                {
                    case "dark":
                        themeColor = "dark";
                        break;
                    case "light":
                        themeColor = "light";
                        break;
                    default:
                        themeColor = partnerInfoData.VSCodeSettings.LoadingScreenThemeColor ?? "light";
                        break;
                }
            }

            ViewData["shimmer-theme-color-class-name"] = (themeColor == "light")
                ? "is-light-theme"
                : "is-dark-theme";

            ViewData["shimmer-logo-class-name"] = (HttpContext.Request.Host.Host.EndsWith("github.dev"))
                ? "is-logo"
                : "";

            ViewData["favicon-url"] = GetFaviconUrl(partnerInfoData);

            return View();
        }

        private string GetFaviconUrl(PartnerInfo partnerInfoData)
        {
            // get favicon from the partner info if possible, if not set, use the vscode one
            // per vscodeChannel, otherwise use vscode stable one
            var stableUrl = "~/vscode-stable-favicon.ico";
            var insiderUrl = "~/vscode-insider-favicon.ico";

            if (partnerInfoData.VSCodeSettings == null)
            {
                return stableUrl;
            }

            var channel = partnerInfoData.VSCodeSettings.VSCodeChannel ?? "stable";
            var favicon = partnerInfoData.Favicon ?? new Favicon
            {
                Stable = stableUrl,
                Insider = insiderUrl,
            };

            return (channel == "stable")
                ? favicon.Stable
                : favicon.Insider;
        }
    }
}