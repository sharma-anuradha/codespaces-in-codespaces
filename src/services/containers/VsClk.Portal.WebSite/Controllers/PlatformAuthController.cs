using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Authentication;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Filters;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class PlatformAuthController : Controller
    {
        [BrandedView]
        [HttpGet("~/platform-auth")]
        public ActionResult Index() => View();

        [HttpPost("~/platform-auth")]
        [Authorize(AuthenticationSchemes = AuthenticationServiceCollectionExtensions.VsoBodyAuthenticationScheme)]
        public async Task<IActionResult> PlatformAuth()
        {
            var claimsIdentity = new ClaimsIdentity(
                HttpContext.User.Claims, AuthenticationServiceCollectionExtensions.CookieNoSameSiteScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2),
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                AuthenticationServiceCollectionExtensions.CookieNoSameSiteScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return View("Index");
        }
    }
}