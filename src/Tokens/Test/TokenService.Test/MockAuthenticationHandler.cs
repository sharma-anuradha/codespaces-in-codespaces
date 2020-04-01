// <copyright file="MockAuthenticationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.VsSaaS.Services.TokenService.Authentication;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    /// <summary>
    /// A mock authentication handler allows the caller to specify arbitrary claims in an
    /// unverified "mock" security token. It should only be enabled in a dev/test environment.
    /// </summary>
    /// <remarks>
    /// To use mock authentication with a VSO API call, include an HTTP header of the form:
    ///     Authorization: Bearer token-claims-base64
    /// The string after "Bearer" is a base64-encoded JSON-formatted object containing the mock
    /// token payload claims. The claims are not validated when mocking, and there is no token
    /// signature. Some claims that are normally required in a JWT ('iss', 'aud', 'nbf', 'exp')
    /// may be omitted, but user identity and other claims required by the service must be supplied.
    /// </remarks>
    internal class MockAuthenticationHandler : AuthenticationHandler<JwtBearerOptions>
    {
        private readonly MockSecurityTokenHandler tokenHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="options">Authentication options.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="encoder">URL encoder.</param>
        /// <param name="clock">System clock.</param>
        public MockAuthenticationHandler(
            IOptionsMonitor<JwtBearerOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            this.tokenHandler = new MockSecurityTokenHandler();
        }

        /// <inheritdoc/>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = Request.Headers[HeaderNames.Authorization].ToString();
            if (!AuthenticationHeaderValue.TryParse(authHeader, out var authHeaderValue))
            {
                return AuthenticateResult.NoResult();
            }

            JwtSecurityToken mockToken;
            ClaimsPrincipal mockPrincipal;
            try
            {
                var mockTokenPayload = JwtPayload.Deserialize(
                    Encoding.UTF8.GetString(Convert.FromBase64String(authHeaderValue.Parameter)));

                if (mockTokenPayload.Iss == null)
                {
                    var mockIssuer = new Uri(new Uri(CurrentUri), "/" + Scheme.Name);
                    mockTokenPayload.AddClaim(new Claim(
                        JwtRegisteredClaimNames.Iss, mockIssuer.AbsoluteUri));
                }

                if (mockTokenPayload.Aud.Count == 0)
                {
                    mockTokenPayload.AddClaim(new Claim(
                        JwtRegisteredClaimNames.Aud,
                        Microsoft.VsSaaS.Common.Identity.AuthenticationConstants.VisualStudioServicesApiAppId));
                }

                mockToken = new JwtSecurityToken(new JwtHeader(), mockTokenPayload);
                mockPrincipal = this.tokenHandler.CreatePrincipal(mockToken);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex);
            }

            await JwtBearerUtility.TokenValidatedAsync(
                new TokenValidatedContext(Request.HttpContext, Scheme, Options)
                {
                    SecurityToken = mockToken,
                    Principal = mockPrincipal,
                });

            return AuthenticateResult.Success(
                new AuthenticationTicket(mockPrincipal, Scheme.Name));
        }

        /// <summary>
        /// Enables bypassing token signature validation to create a principal from
        /// a mock JWT token.
        /// </summary>
        private class MockSecurityTokenHandler : JwtSecurityTokenHandler
        {
            private readonly TokenValidationParameters validationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    RequireAudience = false,
                    RequireExpirationTime = false,
                };

            public ClaimsPrincipal CreatePrincipal(JwtSecurityToken mockToken)
            {
                var issuer = mockToken.Issuer;
                Requires.NotNull(issuer, JwtRegisteredClaimNames.Iss);

                var identity = CreateClaimsIdentity(mockToken, issuer, validationParameters);
                return new ClaimsPrincipal(identity);
            }
        }
    }
}
