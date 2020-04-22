// <copyright file="TokensController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.TokenService.Authentication;
using Microsoft.VsSaaS.Services.TokenService.Contracts;
using Microsoft.VsSaaS.Services.TokenService.Settings;
using Microsoft.VsSaaS.Tokens;
using static Microsoft.VsSaaS.Services.TokenService.Authentication.JwtBearerUtility;
using static Microsoft.VsSaaS.Services.TokenService.Utils;

namespace Microsoft.VsSaaS.Services.TokenService.Controllers
{
    /// <summary>
    /// API for token operations.
    /// </summary>
    [ApiController]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("tokens_controller")]
    [ValidationExceptionFilter]
    public class TokensController : Controller
    {
        private readonly IJwtWriter jwtWriter;
        private readonly IJwtReader jwtReader;
        private readonly IAuthenticationSchemeProvider authSchemeProvider;
        private readonly TokenServiceAppSettings settings;
        private readonly TokenIssuerSettings? exchangeIssuer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokensController"/> class.
        /// </summary>
        /// <param name="jwtWriter">JWT writer.</param>
        /// <param name="jwtReader">JWT reader.</param>
        /// <param name="authSchemeProvider">Authentication scheme provider.</param>
        /// <param name="settings">Token service app settings.</param>
        public TokensController(
            IJwtWriter jwtWriter,
            IJwtReader jwtReader,
            IAuthenticationSchemeProvider authSchemeProvider,
            TokenServiceAppSettings settings)
        {
            this.jwtWriter = Requires.NotNull(jwtWriter, nameof(jwtWriter));
            this.jwtReader = Requires.NotNull(jwtReader, nameof(jwtReader));
            this.authSchemeProvider = Requires.NotNull(
                authSchemeProvider, nameof(authSchemeProvider));
            this.settings = Requires.NotNull(settings, nameof(settings));

            if (!string.IsNullOrEmpty(settings.ExchangeSettings?.Issuer))
            {
                this.settings.IssuerSettings?.TryGetValue(
                    settings.ExchangeSettings.Issuer, out this.exchangeIssuer);
            }
        }

        /// <summary>
        /// Issues a token.
        /// </summary>
        /// <param name="parameters">The token parameters including claims.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>An issue result including the issued token.</returns>
        [HttpPost("issue")]
        [Authorize(AuthenticationSchemes = AadAuthenticationScheme, Roles = IssuerRole)]
        [ProducesResponseType(typeof(IssueResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpOperationalScope("issue")]
        public IActionResult Issue(
            [FromBody]IssueParameters parameters,
            [FromServices]IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(parameters, nameof(parameters));
            ValidationUtil.IsRequired(parameters.Claims, "claims");

            var claims = parameters.Claims.Select((c) => new Claim(c.Key, c.Value));
            var payload = new JwtPayload(claims);

            var issuerSettings = ValidateIssueIssuer(payload.Iss);
            if (issuerSettings == null)
            {
                logger.AddValue("issuer", payload.Iss ?? string.Empty);
                logger.LogError("token_issue_invalid_issuer");
                return Problem("Invalid issuer: " + payload.Iss);
            }
            else if (issuerSettings.IssuerUri != payload.Iss)
            {
                // Fill in default issuer, or replace with normalized issuer.
                payload.Remove(JwtRegisteredClaimNames.Iss);
                payload.AddClaim(new Claim(JwtRegisteredClaimNames.Iss, issuerSettings.IssuerUri));
            }

            if (!ValidateIssueAudiences(payload.Aud))
            {
                logger.AddValue("audience", string.Join(",", payload.Aud));
                logger.LogError("token_issue_invalid_audience");
                return Problem("Invalid audience: " + string.Join(",", payload.Aud));
            }

            if (issuerSettings.MaxLifetime != null)
            {
                // Apply maximum / default expiration for the issuer.
                var maxExpiration = DateTime.UtcNow + issuerSettings.MaxLifetime.Value;
                if (payload.Exp == null || payload.ValidTo > maxExpiration)
                {
                    payload.Remove(JwtRegisteredClaimNames.Exp);
                    payload.AddClaim(JwtWriter.CreateDateTimeClaim(
                        JwtRegisteredClaimNames.Exp,
                        maxExpiration));
                }
            }

            string token;
            try
            {
                token = this.jwtWriter.WriteToken(payload, logger);
            }
            catch (SecurityTokenException ex)
            {
                // Exception details are also logged by WriteToken().
                logger.LogErrorWithDetail("token_issue_invalid_claims", ex.Message);
                return Problem("Invalid token claims.", detail: ex.Message);
            }

            var result = new IssueResult
            {
                Token = token,
            };
            return Ok(result);
        }

        /// <summary>
        /// Validates a token, decrypting if necessary, and returns the extracted claims.
        /// </summary>
        /// <param name="parameters">The validation parameters including a token.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>A validation result including the token claims.</returns>
        [HttpPost("validate")]
        [Authorize(AuthenticationSchemes = AadAuthenticationScheme)]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ValidateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpOperationalScope("validate")]
        public IActionResult Validate(
            [FromBody]ValidateParameters parameters,
            [FromServices]IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(parameters, nameof(parameters));
            ValidationUtil.IsRequired(parameters.Token, "token");

            // Only known services (having the Validator role) are allowed to decrypt
            // tokens. Any other clients can freely validate only unencrypted tokens.
            bool canDecryptTokens = HttpContext.User?.IsInRole(ValidatorRole) == true;

            ValidateResult result;
            try
            {
                if (!canDecryptTokens)
                {
                    JwtPayload unencryptedPayload;
                    try
                    {
                        // DecodeToken() returns unencrypted header claims from an encrypted token.
                        unencryptedPayload = this.jwtReader.DecodeToken(parameters.Token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogErrorWithDetail("token_validate_decode_error", ex.Message);
                        return Problem("Invalid token.", ex.Message);
                    }

                    if (unencryptedPayload.ContainsKey("enc"))
                    {
                        // Not authorized to decrypt tokens.
                        logger.LogError("token_validate_unauthorized");
                        return Unauthorized();
                    }
                }

                var payload = this.jwtReader.ReadTokenPayload(parameters.Token, logger);

                var claims = payload.ToDictionary(
                    (pair) => pair.Key,
                    (pair) => pair.Value?.ToString() ?? string.Empty);

                result = new ValidateResult
                {
                    Claims = claims,
                };
            }
            catch (SecurityTokenException ex)
            {
                result = ValidateResult.FromException(ex);

                // Exception details are also logged by ReadTokenPayload().
                logger.AddValue("validationError", result.Error.ToString());
                logger.LogErrorWithDetail("token_validate_error", result.ErrorMessage);
            }
            catch (ArgumentException ex)
            {
                logger.LogErrorWithDetail("token_validate_error", ex.Message);
                return Problem("Invalid token.", ex.Message);
            }

            return Ok(result);
        }

        /// <summary>
        /// Exchanges a token for a newly issued token with a requested audience.
        /// </summary>
        /// <param name="parameters">The exchange parameters including a token.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>An issue result including the issued token.</returns>
        [HttpPost("exchange")]
        [Authorize(AuthenticationSchemes = AllAuthenticationSchemes)]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IssueResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpOperationalScope("exchange")]
        public async Task<IActionResult> ExchangeAsync(
            [FromBody]ExchangeParameters? parameters,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (this.exchangeIssuer == null || string.IsNullOrEmpty(this.exchangeIssuer.IssuerUri))
            {
                return Problem("Missing exchange issuer configuration.");
            }

            var provider = parameters?.Provider ?? ProviderNames.Microsoft;
            var identity = User.Identity as ClaimsIdentity;

            if (!string.IsNullOrEmpty(parameters?.Token))
            {
                if (identity?.IsAuthenticated == true)
                {
                    return Problem("Include token in either header or body, not both.");
                }

                var authScheme = provider == ProviderNames.Microsoft ? AadAuthenticationScheme : provider;
                var authenticateResult = await ReauthenticateAsync(authScheme, parameters.Token);
                if (!authenticateResult.Succeeded ||
                    (identity = authenticateResult.Principal.Identity as ClaimsIdentity) == null)
                {
                    logger.LogError("token_exchange_unauthorized");
                    return Unauthorized();
                }
            }

            if (identity?.IsAuthenticated != true)
            {
                logger.LogError("token_exchange_missing_claims");
                return Unauthorized();
            }

            var payload = new JwtPayload();
            payload.AddClaim(new Claim(JwtRegisteredClaimNames.Iss, this.exchangeIssuer.IssuerUri));

            var audience = ValidateExchangeAudience(parameters?.Audience);
            if (audience == null)
            {
                logger.AddValue("audience", parameters?.Audience);
                logger.LogError("token_exchange_invalid_audience");
                return Problem("Invalid audience: " + parameters?.Audience ??
                        this.settings.ExchangeSettings?.ValidAudiences?.FirstOrDefault() ??
                        string.Empty);
            }

            payload.AddClaim(new Claim(JwtRegisteredClaimNames.Aud, audience));

            var defaultLifetime = this.settings.ExchangeSettings?.Lifetime ??
                this.exchangeIssuer.MaxLifetime;
            var lifetime = parameters?.Lifetime != null &&
                (defaultLifetime == null || parameters.Lifetime < defaultLifetime) ?
                parameters.Lifetime.Value : defaultLifetime;
            if (lifetime != null)
            {
                var exp = DateTime.UtcNow + lifetime.Value;
                payload.AddClaim(JwtWriter.CreateDateTimeClaim(JwtRegisteredClaimNames.Exp, exp));
            }

            var claimsError = CopyIdentityClaims(provider, identity.Claims, payload, logger);
            if (claimsError != null)
            {
                return claimsError;
            }

            string token;
            try
            {
                token = this.jwtWriter.WriteToken(payload, logger);
            }
            catch (SecurityTokenException ex)
            {
                // Exception details are also logged by WriteToken().
                logger.LogErrorWithDetail("token_issue_invalid_claims", ex.Message);
                return Problem("Invalid token claims.", detail: ex.Message);
            }

            var result = new IssueResult
            {
                Token = token,
            };
            return Ok(result);
        }

        /// <summary>
        /// Attempts to reauthenticate the current request with a token extracted from the body.
        /// </summary>
        private async Task<AuthenticateResult> ReauthenticateAsync(
            string authenticationScheme,
            string token)
        {
            // Place the token into the auth header before re-authenticating the request.
            var headerAuthScheme = authenticationScheme == AadAuthenticationScheme
                ? "Bearer" : authenticationScheme;
            Request.Headers["Authorization"] = $"{headerAuthScheme} {token}";

            // Create a new instance of the authentication handler for the scheme.
            // (The existing instance of the handler for the request has already failed.)
            var scheme = await this.authSchemeProvider.GetSchemeAsync(
                authenticationScheme);
            var handler = (IAuthenticationHandler)ActivatorUtilities.CreateInstance(
                HttpContext.RequestServices, scheme.HandlerType);
            await handler.InitializeAsync(scheme, HttpContext);

            var authenticateResult = await handler.AuthenticateAsync();
            return authenticateResult;
        }

        private TokenIssuerSettings? ValidateIssueIssuer(string? issuer)
        {
            var issuerSettings = this.settings.IssuerSettings ??
                throw new InvalidOperationException("Missing issuer settings.");

            var clientSettings = HttpContext.GetClientSettings();
            var validIssuers = clientSettings?.ValidIssuers ?? Enumerable.Empty<string>();

            foreach (var issuerKey in validIssuers)
            {
                if (issuerSettings.TryGetValue(issuerKey, out var validIssuer) &&
                    !validIssuer.ValidateOnly && (string.IsNullOrEmpty(issuer) ||
                    EqualsIgnoringTrailingSlash(validIssuer.IssuerUri, issuer)))
                {
                    return validIssuer;
                }
            }

            return null;
        }

        private bool ValidateIssueAudiences(ICollection<string> audiences)
        {
            if (audiences == null || audiences.Count == 0)
            {
                // The token issue API will throw an appropriate exception.
                return true;
            }

            var audienceSettings = this.settings.AudienceSettings ??
                throw new InvalidOperationException("Missing audience settings.");

            var clientSettings = HttpContext.GetClientSettings();
            foreach (var audienceKey in clientSettings?.ValidAudiences ??
                Enumerable.Empty<string>())
            {
                if (audienceSettings.TryGetValue(audienceKey, out var validAudience))
                {
                    foreach (var audience in audiences)
                    {
                        if (EqualsIgnoringTrailingSlash(validAudience.AudienceUri, audience))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private string? ValidateExchangeAudience(string? audience)
        {
            var exchangeSettings = this.settings.ExchangeSettings ??
                throw new InvalidOperationException("Missing exchange settings.");
            var audienceSettings = this.settings.AudienceSettings ??
                throw new InvalidOperationException("Missing audience settings.");

            TokenAudienceSettings validAudience;
            if (!string.IsNullOrEmpty(audience))
            {
                // The caller specified an audience.
                string audienceKey;
                (audienceKey, validAudience) = audienceSettings.SingleOrDefault(
                    (a) => EqualsIgnoringTrailingSlash(a.Value.AudienceUri, audience));
                if (audienceKey == null)
                {
                    // The requested audience URI was not found in the list of known audiences.
                    return null;
                }

                if (exchangeSettings.ValidAudiences?.Contains(audienceKey) != true)
                {
                    // The audience is known but is not allowed for exchanging.
                    return null;
                }
            }
            else
            {
                // No audience was specified. Use the default audience for exchanging.
                var audienceKey = exchangeSettings.ValidAudiences?.FirstOrDefault();
                if (audienceKey == null ||
                    !audienceSettings.TryGetValue(audienceKey, out validAudience!))
                {
                    // An unknown audience is configured as the default exchange audience.
                    return null;
                }
            }

            return validAudience.AudienceUri;
        }

        private IActionResult? CopyIdentityClaims(
            string providerName,
            IEnumerable<Claim> fromClaims,
            JwtPayload toPayload,
            IDiagnosticsLogger logger)
        {
            const string tidClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
            const string oidClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

            var clientSettings = HttpContext.GetClientSettings();

            string? GetClaimValue(string claimName, string? altClaimName = null)
            {
                return fromClaims.SingleOrDefault(
                    (c) => c.Type == claimName || c.Type == altClaimName)?.Value;
            }

            var tid = GetClaimValue(CustomClaims.TenantId, tidClaimType);
            if (string.IsNullOrEmpty(tid))
            {
                logger.LogError("token_exchange_missing_tid");
                return Problem($"Missing claim: '{CustomClaims.TenantId}'");
            }

            var oid = GetClaimValue(CustomClaims.OId, oidClaimType);
            if (string.IsNullOrEmpty(oid))
            {
                logger.LogError("token_exchange_missing_oid");
                return Problem($"Missing claim: '{CustomClaims.OId}'");
            }

            toPayload.AddClaim(new Claim(CustomClaims.Provider, providerName));
            toPayload.AddClaim(new Claim(CustomClaims.TenantId, tid));
            toPayload.AddClaim(new Claim(CustomClaims.OId, oid));

            void AddOptionalClaim(string claimName, string? value, string? defaultValue)
            {
                if (string.IsNullOrEmpty(value))
                {
                    if (string.IsNullOrEmpty(defaultValue))
                    {
                        return;
                    }

                    value = defaultValue;
                }

                toPayload.AddClaim(new Claim(claimName, value!));
            }

            var name = GetClaimValue(CustomClaims.DisplayName, ClaimTypes.Name);
            var email = GetClaimValue(CustomClaims.Email, ClaimTypes.Email);
            var username = GetClaimValue(CustomClaims.Username);

            if (string.IsNullOrEmpty(email) && username?.Contains('@') == true)
            {
                email = username;
            }

            AddOptionalClaim(CustomClaims.DisplayName, name, clientSettings?.DisplayName);
            AddOptionalClaim(CustomClaims.Email, email, clientSettings?.Email);
            AddOptionalClaim(CustomClaims.Username, username, null);

            return null;
        }

        private ObjectResult Problem(string title, string? detail = null)
        {
            return Problem(
                title: title,
                detail: detail,
                statusCode: (int)HttpStatusCode.BadRequest);
        }
    }
}
