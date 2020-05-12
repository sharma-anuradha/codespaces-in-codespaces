using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Authentication;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Filters;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

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
    }
}