using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.CookieCompliance;
using Microsoft.CookieCompliance.NetCore;
using Microsoft.CookieCompliance.NetCore.IPAddressResolver;
using Microsoft.CookieCompliance.NetStd.IP2Geo;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class CookieConsentController : Controller
    {
        public CookieConsentController() { }

        /// <summary>
        /// Check locale based on remote IP address and if necessary, retrieve resources to render on the cookie compliance banner.
        /// </summary>
        /// <returns>MSCC cookie consent response</returns>
        [HttpGet("~/cookie-consent")]
        public IActionResult Markup()
        {
            var remoteIpAddress = Request.Headers["X-Forwarded-For"].ToString();
            var acceptLanguage = GetFirstAcceptLanguage();
            return Ok(JsonConvert.SerializeObject(CookieConsentClientSingleton.Instance.GetConsentMarkup(Request.HttpContext, acceptLanguage, remoteIpAddress)));
        }

        /// <summary>
        /// Gets the preferred accept language from the request header
        /// </summary>
        /// <returns>Preferred language or empty string</returns>
        private string GetFirstAcceptLanguage()
        {
            string language = string.Empty;
            var languageHeader = Request.Headers["Accept-Language"];
            if (languageHeader.Count > 0)
            {
                var languages = languageHeader[0];
                var commaIndex = languages.IndexOf(',');
                if (commaIndex > 0)
                {
                    language = languages.Substring(0, commaIndex);
                    if (language.Contains("-"))
                    {
                        language = language.Split("-")[0];
                    }
                }
                else
                {
                    return languages;
                }
            }

            return language;
        }

        /// <summary>
        /// A singleton for checking if cookie consent is required or not, and when required providing the applicable resources (markup, javascript, stylesheets).
        /// See for details: https://www.redtigerwiki.com/wiki/Cookie_consent_API_spec
        /// </summary>
        public sealed class CookieConsentClientSingleton
        {
            private const string _SiteDomain = "online.visualstudio.com";
            private readonly ICookieConsentClient _cookieConsentClient;
            private readonly IPAddressResolver _ipResolver;

            private CookieConsentClientSingleton()
            {
                LoggerFactory factory = new LoggerFactory();
                var logger = factory.CreateLogger("CookieConsentLogger");
                _cookieConsentClient = CookieConsentClientFactory.Create(_SiteDomain, logger);
                _ipResolver = IPAddressResolverFactory.Create(_SiteDomain, logger);
            }

            /// <summary>
            /// Instance of the <see cref="CookieConsentClientSingleton"/>
            /// </summary>
            public static CookieConsentClientSingleton Instance { get; } = new CookieConsentClientSingleton();

            /// <summary>
            /// Get consent markup for the current user.
            /// </summary>
            /// <param name="httpContext">Context passed from the HTTP request</param>
            /// <param name="language">User browser's accept language</param>
            /// <param name="remoteIpAddress">User's remote ip address</param>
            /// <returns>Required resources to render cookie compliance banner, or empty markup.</returns>
            public ConsentMarkup GetConsentMarkup(HttpContext httpContext, string language, string remoteIpAddress)
            {
                ConsentMarkup consentMarkup = new ConsentMarkup();
                var countryCode = GetCountryCode(remoteIpAddress);
                if (IsConsentRequired(httpContext, countryCode))
                {
                    consentMarkup = _cookieConsentClient.GetConsentMarkup(string.IsNullOrEmpty(language) ? countryCode : $"{language}-{countryCode}");
                }
                return consentMarkup;
            }

            /// <summary>
            /// Uses remote IP address to determine country code. If not found, default to an EU region (use "euregion" per docs).
            /// </summary>
            /// <param name="remoteIpAddress">User's remote ip address</param>
            /// <returns>The country code</returns>
            private string GetCountryCode(string remoteIpAddress)
            {
                string countryCode = _ipResolver.GetCountryCode(remoteIpAddress);

                // If country code is null, set to EU to be safe
                // Check cookie to enable manual testing in deployed sites.
                if (string.IsNullOrEmpty(countryCode))
                {
                    countryCode = "euregion";
                }

                return countryCode;
            }

            /// <summary>
            /// Uses country code to determine if consent is required
            /// </summary>
            /// <param name="httpContext">Context passed from the HTTP request</param>
            /// <param name="countryCode">The country code of current user</param>
            /// <returns>True if consent required, false if not required</returns>
            private bool IsConsentRequired(HttpContext httpContext, string countryCode)
            {
                var consentStatus = _cookieConsentClient.IsConsentRequiredForRegion(_SiteDomain, countryCode, httpContext);
                return consentStatus == ConsentStatus.Required;
            }
        }
    }
}
