using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Authenticate tag class used for ILogger<T>
    /// </summary>
    internal class Authenticate
    {
    }

    /// <summary>
    /// Helper class to enable authentication based on an external profile service Uri
    /// The service Uri will reject non authroized token but also return the proper normalized userId
    /// </summary>
    public static class AuthenticateProfileServiceExtension
    {
        private const int MaxTokenExpired = 2000;
        private static int tokenExpiredCounter = 0;

        public static void AddProfileServiceJwtBearer(
            this IServiceCollection services,
            string authenticateProfileServiceUri,
            ILogger logger,
            string agentId)
        {
            // Create a token cache
            var tokenCache = new ConcurrentDictionary<string, (DateTime, ClaimsPrincipal)>();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = (msgCtxt) => OnMessageReceivedAsync(msgCtxt, authenticateProfileServiceUri, logger, agentId, tokenCache)
                    };
                });
        }

        private static async Task OnMessageReceivedAsync(
            MessageReceivedContext context,
            string authenticateProfileServiceUri,
            ILogger logger,
            string agentId,
            ConcurrentDictionary<string, (DateTime, ClaimsPrincipal)> tokenCache)
        {
            // If application retrieved token from somewhere else, use that.
            var token = context.Token;

            if (string.IsNullOrEmpty(token))
            {
                string authorization = context.Request.Headers["Authorization"];

                // If no authorization header found, nothing to process further
                if (string.IsNullOrEmpty(authorization))
                {
                    context.NoResult();
                    return;
                }

                if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authorization.Substring("Bearer ".Length).Trim();
                }

                // If no token found, no further work possible
                if (string.IsNullOrEmpty(token))
                {
                    context.NoResult();
                    return;
                }
            }

            // check if our token cache has what we need
            if (tokenCache.TryGetValue(token, out var item))
            {
                context.Principal = item.Item2;
                context.Success();
                return;
            }
            else
            {
                // purge old tokens
                var expiredThreshold = DateTime.Now.Subtract(TimeSpan.FromSeconds(30));
                var expiredCacheItemsKeys = tokenCache.Where(kvp => kvp.Value.Item1 < expiredThreshold).Select(kvp => kvp.Key).ToArray();
                foreach(var key in expiredCacheItemsKeys)
                {
                    tokenCache.TryRemove(key, out var itemRemoved);
                }
            }

            var isTokenExpired = false;
            try
            {
                var httpClientFactory = context.HttpContext.RequestServices.GetService<IHttpClientFactory>();

                // Next block will retrieve a profile from the configured Uri service
                var httpClient = httpClientFactory.CreateClient("auth");

                // define Authorization
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // define UserAgent
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue(agentId)));

                var response = await httpClient.GetAsync(authenticateProfileServiceUri);

                // update if token is expired to avoid noise on telemetry
                isTokenExpired = response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    IsTokenExpired(response);

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                var profile = JObject.Parse(json);
                var userId = profile["id"].ToString();
                var email = profile["email"].ToString();

                using (logger.BeginSingleScope(
                    (LoggerScopeHelpers.MethodScope, "AuthenticateProfile")))
                {
                    logger.LogDebug($"Successfully authorized userId:{userId} email:{email}");
                }

                var claims = new Claim[] {
                    new Claim("userId", userId, ClaimValueTypes.String),
                    new Claim(ClaimTypes.Email, email, ClaimValueTypes.String),
                };

                var userIdentity = new ClaimsIdentity(claims, "Passport");
                context.Principal = new ClaimsPrincipal(userIdentity);
                context.Success();

                // cache our token
                tokenCache.TryAdd(token, (DateTime.Now, context.Principal));
            }
            catch (Exception error)
            {
                if (!isTokenExpired)
                {
                    using (logger.BeginSingleScope(
                        (LoggerScopeHelpers.MethodScope, "AuthenticateFailed")))
                    {
                        logger.LogError(error, $"Error when retrieving profile from Url:'{authenticateProfileServiceUri}'");
                    }
                }
                else if ((System.Threading.Interlocked.Increment(ref tokenExpiredCounter) % MaxTokenExpired) == 0)
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
                    using (logger.BeginSingleScope(
                       (LoggerScopeHelpers.MethodScope, "AuthenticateExpired")))
                    {
                        logger.LogWarning($"Token expired -> id:{jwtToken.Id} issuer:{jwtToken.Issuer} from:{jwtToken.ValidFrom} to:{jwtToken.ValidTo}");
                    }
                }
                context.Fail(error);
            }
        }

        private static bool IsTokenExpired(HttpResponseMessage response)
        {
            const string ExpiredParameter = "The token is expired";
            return response.Headers.WwwAuthenticate.Any(h => h.Scheme == "Bearer" && h.Parameter?.Contains(ExpiredParameter) == true);
        }
    }
}
