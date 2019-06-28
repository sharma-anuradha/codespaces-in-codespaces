using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Helper class to enable authentication based on an external profile service Uri
    /// The service Uri will reject non authroized token but also return the proper normalized userId
    /// </summary>
    public static class AuthenticateProfileServiceExtension
    {
        public static void AddProfileServiceJwtBearer(
            this IServiceCollection services,
            string authenticateProfileServiceUri,
            ILogger logger)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = (msgCtxt) => OnMessageReceivedAsync(msgCtxt, authenticateProfileServiceUri, logger)
                    };
                });
        }

        private static async Task OnMessageReceivedAsync(
            MessageReceivedContext context,
            string authenticateProfileServiceUri,
            ILogger logger)
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

            try
            {
                var httpClientFactory = context.HttpContext.RequestServices.GetService<IHttpClientFactory>();

                // Next block will retrieve a profile from the configured Uri service
                var httpClient = httpClientFactory.CreateClient("auth");

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.GetAsync(authenticateProfileServiceUri);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                var profile = JObject.Parse(json);
                var userId = profile["id"].ToString();
                var email = profile["email"].ToString();

                logger.LogDebug($"Successfully authorized userId:{userId} email:{email}");

                var claims = new Claim[] {
                    new Claim("userId", userId, ClaimValueTypes.String),
                    new Claim(ClaimTypes.Email, email, ClaimValueTypes.String),
                };

                var userIdentity = new ClaimsIdentity(claims, "Passport");
                context.Principal = new ClaimsPrincipal(userIdentity);

                context.Success();
            }
            catch (Exception error)
            {
                logger.LogError(error, $"Error when retrieving profile from Url:'{authenticateProfileServiceUri}'");
                context.Fail(error);
            }
        }
    }
}
