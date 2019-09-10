// <copyright file="JwtTokenGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Diagnostics;

#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1629 // End with a period

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
        /// The libtrust kid of the private key that is used to sign issued JWTs.
        /// </summary>
        private readonly string primarySigningKid;
        private readonly SecurityKey[] primarySigningKey;
        private readonly SigningCredentials primarySigningCredentials;
        private readonly string secondarySigningKid;
        private readonly SecurityKey[] secondarySigningKey;
        private readonly IDiagnosticsLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenGenerator"/> class.
        /// </summary>
        /// <param name="primaryCertificate">Raw bytes of the primary certificate.</param>
        /// <param name="secondaryCertificate">Raw bytes of the secondary certificate.</param>
        /// <param name="logger">Diagnostics logger.</param>
        public JwtTokenGenerator(
            [ValidatedNotNull] byte[] primaryCertificate,
            [ValidatedNotNull] byte[] secondaryCertificate,
            [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            Requires.NotNull(primaryCertificate, nameof(primaryCertificate));
            Requires.NotNull(secondaryCertificate, nameof(secondaryCertificate));
            this.logger = Requires.NotNull(logger, nameof(logger));

            // primary key, for signing and validation
            try
            {
                this.primarySigningKid = Certificates.GenerateKidForPublicKey(primaryCertificate);
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_primary_public_key");
                throw;
            }

            try
            {
                var primaryKey = new RsaSecurityKey(Certificates.GetRSAPrivateKey(primaryCertificate));
                this.primarySigningKey = new[] { primaryKey };
                this.primarySigningCredentials = GetRsaSigningCredentials(primaryKey);
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_primary_private_key");
                throw;
            }

            // secondary key, for validation purposes only
            try
            {
                this.secondarySigningKid = Certificates.GenerateKidForPublicKey(secondaryCertificate);
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_secondary_public_key");
                throw;
            }

            try
            {
                var secondaryKey = new RsaSecurityKey(Certificates.GetRSAPrivateKey(secondaryCertificate));
                this.secondarySigningKey = new[] { secondaryKey };
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_secondary_private_key");
                throw;
            }
        }

        /// <summary>
        /// Create a JWT payload.
        /// </summary>
        /// <param name="issuer">Issuer of the JWT token.</param>
        /// <param name="audience">Audience of the JWT token.</param>
        /// <param name="subject">Subject in the JWT token.</param>
        /// <param name="customClaims">Custom claims to be added.</param>
        /// <param name="expiration">Expirate of the JWT token.</param>
        /// <returns>
        /// Returns a new JwtPayload instance with the basic fields filled out. 
        /// In particular, it ensures to have an issuer, an audience, notBefore time, expires time,
        /// issuedAt time and a jti claim with a GUID.
        /// </returns>
        public JwtPayload NewJwtPayload(string issuer, string audience, string subject, Dictionary<string, string> customClaims, DateTime expiration)
        {
            Requires.NotNullOrWhiteSpace(issuer, nameof(issuer));
            Requires.NotNullOrWhiteSpace(audience, nameof(audience));
            Requires.NotNullOrWhiteSpace(subject, nameof(subject));

            var utcNow = DateTime.UtcNow;
            var issuedAt = utcNow.Add(TokenIssuedAtClockSkew);
            var jwtid = Guid.NewGuid().ToString();

            this.logger.AddValue("jwtid", jwtid);

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
            var header = new JwtHeader(this.primarySigningCredentials)
            {
                { "kid", this.primarySigningKid },
            };

            var token = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Creates a signing credential out of an RS256 key.
        /// </summary>
        private static SigningCredentials GetRsaSigningCredentials(RsaSecurityKey rs256key)
        {
            return new SigningCredentials(rs256key, "RS256");
        }

        /// <summary>
        /// Gets the private key contained in the bytes, extracts the private
        /// key, and creates an RsaSecurityKey instance with it.
        /// </summary>
        private static RsaSecurityKey GetSigningKey(byte[] certBytes)
        {
            return new RsaSecurityKey(Certificates.GetRSAPrivateKey(certBytes));
        }

        private static RsaSecurityKey GetSigningKey(RSA rsa)
        {
            return new RsaSecurityKey(rsa);
        }

        /// <summary>
        /// Returns a set of security keys that will be used to validate a JWT token.
        /// It only returns the keys if the kid provided matches the signing kid.
        /// Note that this needs to be replaced by a comparison to a set of signing
        /// kids in the future when we expand to support multiple keys.
        /// </summary>
        private SecurityKey[] CustomSigningKeyResolver(string tkn, SecurityToken stkn, string kid, TokenValidationParameters vp)
        {
            if (kid == this.primarySigningKid)
            {
                return this.primarySigningKey;
            }

            if (kid == this.secondarySigningKid)
            {
                return this.secondarySigningKey;
            }

            return null;
        }

        /// <summary>
        /// Validates a JWT token. If successful, will return the JWT payload data.
        /// </summary>
        /// <param name="token">Token to be validated.</param>
        /// <param name="issuer">Issuer of the token.</param>
        /// <param name="audience">Audience of the token.</param>
        /// <returns>Validates the JWT token and returns non-null payload if successful.</returns>
        public JwtPayload ValidateJwt(string token, string issuer, string audience)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidAudience = audience,
                ValidIssuer = issuer,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeyResolver = CustomSigningKeyResolver,
            };

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
    }
}