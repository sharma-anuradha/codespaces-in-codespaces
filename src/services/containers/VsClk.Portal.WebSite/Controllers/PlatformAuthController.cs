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

         
        public class GitCredential
        {
            [JsonProperty("expiration")]
            public double Expiration { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("host")]
            public string Host { get; set; }

            [JsonProperty("token")]
            public string Token { get; set; }
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

            [JsonProperty("credentials")]
            public List<GitCredential> Credentials { get; set; }
        }

        [HttpPost("~/platform-authentication")]
        [Authorize(AuthenticationSchemes = AuthenticationServiceCollectionExtensions.VsoBodyAuthenticationScheme)]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult PlatformAuthentication(
            [FromForm] string partnerInfo,
            [FromForm] string cascadeToken
        )
        {
            if (string.IsNullOrWhiteSpace(partnerInfo))
            {
                return BadRequest("No `partnerInfo` set in the body.");
            }

            var partnerInfoData = JsonConvert.DeserializeObject<PartnerInfo>(partnerInfo);
            partnerInfoData.CascadeToken = cascadeToken;

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

            return View();
        }
    }
}