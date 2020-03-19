using System;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class AzureDevopsOAuthController : Controller
    {
        private AppSettings AppSettings { get; set; }

        private const string ClientFlowFeedbackEndpoint = "azdev/login";
        private const string AzureDevOpsAppClientScope = "vso.code_full vso.code_status";

        public AzureDevopsOAuthController(AppSettings appSettings)
        {
            AppSettings = appSettings;
        }

        [HttpGet("~/azdev-auth/")]
        public IActionResult Authenticate(
            [FromQuery(Name = "state")] string state
        )
        {
            return new RedirectResult(GetAuthorizationUrl(state));
        }

        [HttpGet("~/azdev-auth/callback")]
        public async Task<IActionResult> Callback(
            [FromQuery(Name = "code")] string code,
            [FromQuery(Name = "state")] string state
        )
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrEmpty(state))
            {
                return BadRequest();
            }

            var responseQuery = HttpUtility.ParseQueryString(string.Empty);

            // Exchange the auth code for an access token and refresh token
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, Constants.AzureDevOpsTokenURL);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Dictionary<string, string> form = new Dictionary<string, string>()
            {
                { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                { "client_assertion", AppSettings.AzDevAppClientSecret },
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "assertion", code },
                { "redirect_uri", GetCallbackUrl() }
            };
            requestMessage.Content = new FormUrlEncodedContent(form);

            HttpClient client = new HttpClient();

            HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);
            responseMessage.EnsureSuccessStatusCode();

            string body = await responseMessage.Content.ReadAsStringAsync();

            TokenModel tokenModel = JsonConvert.DeserializeObject<TokenModel>(body);

            responseQuery.Set("state", state.ToString());
            responseQuery.Set("accessToken", tokenModel.AccessToken);
            responseQuery.Set("refreshToken", tokenModel.RefreshToken);
            responseQuery.Set("expiresIn", tokenModel.ExpiresIn.ToString());
            responseQuery.Set("scope", AzureDevOpsAppClientScope);

            var responseUriBuilder = GetUriBuilder();
            responseUriBuilder.Path = ClientFlowFeedbackEndpoint;
            responseUriBuilder.Query = responseQuery.ToString();

            return Redirect(responseUriBuilder.Uri.ToString());
        }

        [HttpGet("~/azdev-auth/getAccessTokenFromRefreshToken")]
        public async Task<IActionResult> GetAccessTokenFromRefreshToken(
            [FromQuery(Name = "refreshToken")] string refreshToken
        )
        {
            // Exchange the auth code for an access token and refresh token
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, Constants.AzureDevOpsTokenURL);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Dictionary<string, string> form = new Dictionary<string, string>()
            {
                { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                { "client_assertion", AppSettings.AzDevAppClientSecret },
                { "grant_type", "refresh_token" },
                { "assertion", refreshToken },
                { "redirect_uri", GetCallbackUrl() }
            };
            requestMessage.Content = new FormUrlEncodedContent(form);
            HttpClient client = new HttpClient();

            HttpResponseMessage httpResponseMessage = await client.SendAsync(requestMessage);
            httpResponseMessage.EnsureSuccessStatusCode();

            string responseMessage = await httpResponseMessage.Content.ReadAsStringAsync();

            return Ok(responseMessage);
        }

        private string GetAuthorizationUrl(string state)
        {
            UriBuilder uriBuilder = new UriBuilder(Constants.AzureDevOpsAuthorizeURL);
            var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query ?? string.Empty);

            queryParams["client_id"] = AppSettings.AzDevAppClientId;
            queryParams["response_type"] = "Assertion";
            queryParams["state"] = state;
            queryParams["scope"] = AzureDevOpsAppClientScope;
            queryParams["redirect_uri"] = GetCallbackUrl();

            uriBuilder.Query = queryParams.ToString();

            return uriBuilder.ToString();
        }

        private string GetCallbackUrl()
        {
            var redirectUriBuilder = GetUriBuilder();
            redirectUriBuilder.Path = "/azdev-auth/callback";

            return redirectUriBuilder.Uri.ToString();
        }

        private UriBuilder GetUriBuilder()
        {
            if (Request.Host.HasValue && Request.Host.Port.HasValue)
            {
                return new UriBuilder(Request.Scheme, Request.Host.Host, Request.Host.Port.Value);
            }
            else
            {
                return new UriBuilder(Request.Scheme, Request.Host.Host);
            }
        }
    }


    [DataContract]
    public class TokenModel
    {
        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }

        [DataMember(Name = "token_type")]
        public string TokenType { get; set; }

        [DataMember(Name = "refresh_token")]
        public string RefreshToken { get; set; }

        [DataMember(Name = "expires_in")]
        public int ExpiresIn { get; set; }

        public bool IsPending { get; set; }
    }
}
