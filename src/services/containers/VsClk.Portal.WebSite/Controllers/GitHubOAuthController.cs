using System;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class GitHubOAuthController : Controller
    {
        private AppSettings AppSettings { get; set; }
        private GitHubClient GitHubClient { get; set; }

        private const string ClientFlowFeedbackEndpoint = "github/login";

        public GitHubOAuthController(AppSettings appSettings)
        {
            AppSettings = appSettings;
            GitHubClient = new GitHubClient(AppSettings.GitHubAppClientId, AppSettings.GitHubAppClientSecret);
        }

        [HttpGet("~/github-auth/")]
        public IActionResult Authenticate(
            [FromQuery(Name = "state")] string state
        )
        {
            if (string.IsNullOrWhiteSpace(state)) {
                return BadRequest();
            }

            var redirectUriBuilder = GetUriBuilder();
            redirectUriBuilder.Path = "/github-auth/access-token";

            return Redirect(
                GitHubClient.GetLoginUrl(
                    state, 
                    redirectUriBuilder.Uri.ToString()));
        }

        [HttpGet("~/github-auth/access-token")]
        public async Task<IActionResult> GetAccessTokenAsync(
            [FromQuery(Name = "code")] string code,
            [FromQuery(Name = "state")] string state
        )
        {
            if (string.IsNullOrWhiteSpace(code)) {
                return BadRequest();
            }
            if (string.IsNullOrWhiteSpace(state)) {
                return BadRequest();
            }

            var responseQuery = HttpUtility.ParseQueryString(string.Empty);
            try
            {
                var tokenResponse = await GitHubClient.GetAccessTokenResponseAsync(state, code);
                responseQuery.Set("state", state);
                responseQuery.Set("accessToken", tokenResponse.AccessToken);
            }
            catch (Exception)
            {
                responseQuery.Add("errorMessage", "Failed to get token from GitHub");
            }

            var responseUriBuilder = GetUriBuilder();
            responseUriBuilder.Path = ClientFlowFeedbackEndpoint;
            responseUriBuilder.Query = responseQuery.ToString();

            return Redirect(responseUriBuilder.Uri.ToString());
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

    class GitHubClient
    {
        private const string LoginEndpoint = "login/oauth/authorize";
        private const string AccessTokenEndpoint = "login/oauth/access_token";
        private const string Scope = "repo";

        public string ClientId { get; }
        public string ClientSecret { get; }

        public GitHubClient(string clientId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new ArgumentNullException(nameof(clientSecret));
            }

            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public string GetLoginUrl(string state, string redirectUrl)
        {
            if (string.IsNullOrEmpty(state))
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (string.IsNullOrEmpty(redirectUrl))
            {
                throw new ArgumentNullException(nameof(redirectUrl));
            }

            var uriBuilder = GetGitHubUriBuilder();
            uriBuilder.Path = LoginEndpoint;

            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Set("client_id", ClientId);
            query.Set("scope", Scope);
            query.Set("redirect_uri", redirectUrl);
            query.Set("state", state);
            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri.ToString();
        }

        public async Task<GitHubAccessTokenResponse> GetAccessTokenResponseAsync(string state, string code)
        {
            if (string.IsNullOrEmpty(state))
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            var uriBuilder = GetGitHubUriBuilder();
            uriBuilder.Path = AccessTokenEndpoint;

            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Set("client_id", ClientId);
            query.Set("client_secret", ClientSecret);
            query.Set("state", state);
            query.Set("code", code);
            uriBuilder.Query = query.ToString();

            HttpClient client = new HttpClient();

            var response = await client.PostAsync(uriBuilder.Uri.ToString(), null);
            response.EnsureSuccessStatusCode();

            // https://developer.github.com/apps/building-oauth-apps/authorizing-oauth-apps/
            // "access_token=91fb03c...&scope=repo&token_type=bearer"
            var responseData = await response.Content.ReadAsStringAsync();
            var parsedData = HttpUtility.ParseQueryString(responseData);

            var accessToken = parsedData.Get("access_token");
            var scope = parsedData.Get("scope");
            var tokenType = parsedData.Get("token_type");

            if (
                string.IsNullOrWhiteSpace(accessToken) ||
                string.IsNullOrWhiteSpace(scope) ||
                string.IsNullOrWhiteSpace(tokenType)
            )
            {
                throw new Exception("Invalid response from GitHub");
            }

            return new GitHubAccessTokenResponse(
                accessToken,
                scope,
                tokenType);
        }

        private UriBuilder GetGitHubUriBuilder()
        {
            return new UriBuilder("https://github.com");
        }

        public class GitHubAccessTokenResponse
        {
            public string AccessToken { get; }
            public string Scope { get; }
            public string TokenType { get; }

            public GitHubAccessTokenResponse(string accessToken, string scope, string tokenType)
            {
                AccessToken = accessToken;
                Scope = scope;
                TokenType = tokenType;
            }
        }
    }

}
