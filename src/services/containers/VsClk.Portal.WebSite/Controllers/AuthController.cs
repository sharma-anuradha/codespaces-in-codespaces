using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Extensions;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class AuthController : Controller
    {
        private AppSettings AppSettings { get; }

        public AuthController(
            AppSettings appSettings)
        {
            AppSettings = appSettings;
        }

        [HttpPost("~/signin")]
        public async Task<IActionResult> SignIn([FromForm] string provider)
        {
            // Note: the "provider" parameter corresponds to the external
            // authentication provider choosen by the user agent.
            if (string.IsNullOrWhiteSpace(provider))
            {
                return BadRequest();
            }

            if (!await HttpContext.IsProviderSupportedAsync(provider))
            {
                return BadRequest();
            }

            // Instruct the middleware corresponding to the requested external identity
            // provider to redirect the user agent to its own authorization endpoint.
            // Note: the authenticationScheme parameter must match the value configured in Startup.cs
            return Challenge(new AuthenticationProperties { RedirectUri = AppSettings.AuthRedirectUrl }, provider);
        }

        [HttpGet("~/signin")]
        public async Task<IActionResult> SignInDefault()
        {

            // Instruct the middleware corresponding to the requested external identity
            // provider to redirect the user agent to its own authorization endpoint.
            // Note: the authenticationScheme parameter must match the value configured in Startup.cs
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, "Microsoft");
        }


        [Authorize]
        [HttpGet("~/api/authorize")]
        public async Task<UserToken> Authorize()
        {
            var userToken = new UserToken();
            if (User.Identity.IsAuthenticated)
            {
                string accessToken = await HttpContext.GetTokenAsync("access_token");
                string idToken = await HttpContext.GetTokenAsync("id_token");
                userToken.AccessToken = accessToken;
                userToken.IdToken = idToken;
                userToken.Name = User.Identity.Name;
            }
            return userToken;
        }

        [HttpGet("~/signout"), HttpPost("~/signout")]
        public IActionResult SignOut()
        {
            // Instruct the cookies middleware to delete the local cookie created
            // when the user agent is redirected from the external identity provider
            // after a successful authentication flow (e.g Google or Facebook).
            return SignOut(new AuthenticationProperties { RedirectUri = "/" },
                CookieAuthenticationDefaults.AuthenticationScheme);
        }


        public class UserToken
        {
            public string AccessToken { get; set; }
            public string IdToken { get; set; }
            public string Name { get; set; }
        }
    }
}
