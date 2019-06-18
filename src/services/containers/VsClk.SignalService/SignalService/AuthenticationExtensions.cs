using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Interface to provide Jwt Certificate validation
    /// </summary>
    public interface ITokenValidationProvider
    {
        string Audience { get; }
        string Issuer { get; }
        SecurityKey[] SecurityKeys { get; }
    }

    /// <summary>
    /// This extension class will allow our presence service to validate liveshare tokens that were created with
    /// our deployed certificate. Tm make this to work our deployment must have the same certifcates to make the
    /// token validation to succeed
    /// </summary>
    public static class AuthenticationExtensions
    {
        private readonly static string RawJwtUserIdKey = "userId";
        public readonly static string RawJwtUserAnonymousKey = "anonymous";

        public static void AddAuthenticationServices(
            this IServiceCollection services,
            ITokenValidationProvider tokenValidationProvider,
            ILogger logger)
        {
            var invalidSecurityKeySet = new SecurityKey[0];

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeyResolver = (string tkn, SecurityToken stkn, string kid, TokenValidationParameters vp) =>
                        {
                            var jwtToken = stkn as JwtSecurityToken;
                            var payload = jwtToken?.Payload as JwtPayload;
                            if (payload == null ||
                                !payload.ContainsKey(RawJwtUserIdKey) ||
                                string.IsNullOrEmpty(payload[RawJwtUserIdKey] as string))
                            {
                                return invalidSecurityKeySet;
                            }

                            // refresh  Audience & Issuer from our provider
                            vp.ValidAudience = tokenValidationProvider.Audience;
                            vp.ValidIssuer = tokenValidationProvider.Issuer;
                            return tokenValidationProvider.SecurityKeys;
                        },
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = (ctx) =>
                        {
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = (afctx) =>
                        {
                            if (!(afctx.Exception is SecurityTokenExpiredException))
                            {
                                logger.LogError(afctx.Exception, "Authentication failed");
                            }

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = (tctx) =>
                        {
                            return Task.CompletedTask;
                        }
                    };
                });
        }
    }
}
