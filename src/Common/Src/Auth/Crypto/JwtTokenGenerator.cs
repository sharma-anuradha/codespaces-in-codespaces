// <copyright file="JwtTokenGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Models;

namespace Microsoft.VsSaaS.Services.Common.Crypto.Utilities
{
    /// <summary>
    /// Utility class to generate and validate JWT tokens provided certificates.
    /// </summary>
    public class JwtTokenGenerator
    {
        /// <summary>
        /// When creating a new token, we must "skew" the time backwards at which it's been
        /// created so that they are valid even if the verifier has no clock-skewing
        /// in place. By default we "issue" the token 15 minutes in the past.
        /// </summary>
        private static readonly TimeSpan TokenIssuedAtClockSkew = TimeSpan.FromMinutes(-15);

        /// <summary>
        /// Credentials that is used for signing and validating the token.
        /// </summary>
        private readonly Credentials primary;

        /// <summary>
        /// Credentials that are used only for validation.
        /// </summary>
        private readonly List<Credentials> secondaries;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenGenerator"/> class.
        /// </summary>
        /// <param name="validCertificates">Valid set of primary and secondary certificates.</param>
        /// <param name="logger">logger.</param>
        public JwtTokenGenerator(
            ValidCertificates validCertificates,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(validCertificates, nameof(validCertificates));

            primary = GetCredentials(validCertificates.Primary.RawBytes, createSigningCredentials: true, logger);
            secondaries = validCertificates.Secondaries.Select(c => GetCredentials(c.RawBytes, false, logger)).ToList();
        }

        /// <summary>
        /// Create a JWT payload.
        /// </summary>
        /// <param name="issuer">Issuer of the JWT token.</param>
        /// <param name="audience">Audience of the JWT token.</param>
        /// <param name="subject">Subject in the JWT token.</param>
        /// <param name="customClaims">Custom claims to be added.</param>
        /// <param name="expiration">Expirate of the JWT token.</param>
        /// <param name="logger">logger.</param>
        /// <returns>
        /// Returns a new JwtPayload instance with the basic fields filled out.
        /// In particular, it ensures to have an issuer, an audience, notBefore time, expires time,
        /// issuedAt time and a jti claim with a GUID.
        /// </returns>
        public JwtPayload NewJwtPayload(string issuer, string audience, string subject, Dictionary<string, string> customClaims, DateTime expiration, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrWhiteSpace(issuer, nameof(issuer));
            Requires.NotNullOrWhiteSpace(audience, nameof(audience));
            Requires.NotNullOrWhiteSpace(subject, nameof(subject));
            Requires.NotNull(logger, nameof(logger));

            var utcNow = DateTime.UtcNow;
            var issuedAt = utcNow.Add(TokenIssuedAtClockSkew);
            var jwtid = Guid.NewGuid().ToString();

            logger.AddValue("jwt_jti", jwtid);
            logger.AddValue("jwt_sub", subject);

            var claims = new List<Claim>
            {
                new Claim("jti", jwtid),
                new Claim("sub", subject),
            };

            if (customClaims != null)
            {
                foreach (var customClaim in customClaims)
                {
                    claims.Add(new Claim(customClaim.Key, customClaim.Value));
                }
            }

            return new JwtPayload(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: issuedAt,
                expires: expiration,
                issuedAt: issuedAt);
        }

        /// <summary>
        /// Takes a JwtPayload instance and creates a secure token signed with the configured certificate
        /// and indicating the currently configured "kid" in the header.
        /// </summary>
        /// <param name="payload">Payload to be signed.</param>
        /// <returns>Token created by signing the payload.</returns>
        public string WriteToken(JwtPayload payload)
        {
            var header = new JwtHeader(this.primary.SigningCredentials)
            {
                { "kid", this.primary.SigningKeyId },
            };

            var token = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Validates a JWT token. If successful, will return the JWT payload data.
        /// </summary>
        /// <param name="token">Token to be validated.</param>
        /// <param name="issuer">Issuer of the token.</param>
        /// <param name="audience">Audience of the token.</param>
        /// <returns>Validates the JWT token and returns non-null payload if successful.</returns>
        public JwtPayload GetJwtPayload(string token, string issuer, string audience)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var handler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters(issuer, audience);

            try
            {
                handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                var jwtToken = validatedToken as JwtSecurityToken;
                return jwtToken?.Payload as JwtPayload;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets token validation parameters.
        /// </summary>
        /// <param name="issuer">issuer.</param>
        /// <param name="audience">audience.</param>
        /// <returns>Token validation parameters.</returns>
        public TokenValidationParameters GetTokenValidationParameters(string issuer, string audience)
        {
            return new TokenValidationParameters
            {
                ValidAudience = audience,
                ValidIssuer = issuer,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeyResolver = CustomSigningKeyResolver,
            };
        }

        /// <summary>
        /// Creates a signing credential out of an RS256 key.
        /// </summary>
        private static SigningCredentials GetRsaSigningCredentials(RsaSecurityKey rs256key)
        {
            return new SigningCredentials(rs256key, "RS256");
        }

        /// <summary>
        /// Returns a set of security keys that will be used to validate a JWT token.
        /// It only returns the keys if the kid provided matches the signing kid.
        /// </summary>
        private SecurityKey[] CustomSigningKeyResolver(string tkn, SecurityToken stkn, string kid, TokenValidationParameters validationParameters)
        {
            if (kid == this.primary.SigningKeyId)
            {
                return this.primary.SigningKey;
            }

            return this.secondaries
                .SingleOrDefault(s => (kid == s.SigningKeyId))
                ?.SigningKey;
        }

        private Credentials GetCredentials(byte[] rawBytes, bool createSigningCredentials, IDiagnosticsLogger logger)
        {
            Credentials credentials = new Credentials();
            try
            {
                credentials.SigningKeyId = Certificates.GenerateKidForPublicKey(rawBytes);
            }
            catch (Exception e)
            {
                logger.WithValue("Exception", e.ToString()).LogError("error_reading_public_key");
                throw;
            }

            try
            {
                var signingKey = new RsaSecurityKey(Certificates.GetRSAPrivateKey(rawBytes));
                credentials.SigningKey = new[] { signingKey };

                if (createSigningCredentials)
                {
                    credentials.SigningCredentials = GetRsaSigningCredentials(signingKey);
                }
            }
            catch (Exception e)
            {
                logger.WithValue("Exception", e.ToString()).LogError("error_reading_private_key");
                throw;
            }

            return credentials;
        }

        private class Credentials
        {
            public string SigningKeyId { get; set; }

            public SecurityKey[] SigningKey { get; set; }

            public SigningCredentials SigningCredentials { get; set; }
        }
    }
}