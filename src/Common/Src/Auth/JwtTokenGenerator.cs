// <copyright file="JwtTokenGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.Common.Crypto.Utilities
{
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

        public JwtTokenGenerator(
            [ValidatedNotNull] byte[] primaryPublicKeyBytes,
            [ValidatedNotNull] byte[] primaryPrivateKeyBytes,
            [ValidatedNotNull] byte[] secondaryPublicKeyBytes,
            [ValidatedNotNull] byte[] secondaryPrivateKeyBytes,
            [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            Requires.NotNull(primaryPublicKeyBytes, nameof(primaryPublicKeyBytes));
            Requires.NotNull(primaryPrivateKeyBytes, nameof(primaryPrivateKeyBytes));
            Requires.NotNull(secondaryPublicKeyBytes, nameof(secondaryPublicKeyBytes));
            Requires.NotNull(secondaryPrivateKeyBytes, nameof(secondaryPrivateKeyBytes));
            Requires.NotNull(logger, nameof(logger));

            this.logger = logger;

            // primary key, for signing and validation
            try
            {
                this.primarySigningKid = Certificates.GenerateKidForPublicKey(primaryPublicKeyBytes);
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_primary_public_key");
                throw;
            }

            try
            {
                var key = GetSigningKey(primaryPrivateKeyBytes);
                this.primarySigningKey = new[] { key };
                this.primarySigningCredentials = GetRsaSigningCredentials(key);
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_primary_private_key");
                throw;
            }

            // secondary key, for validation purposes only
            try
            {
                this.secondarySigningKid = Certificates.GenerateKidForPublicKey(secondaryPublicKeyBytes);
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_secondary_public_key");
                throw;
            }

            try
            {
                var key = GetSigningKey(secondaryPrivateKeyBytes);
                this.secondarySigningKey = new[] { key };
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_secondary_private_key");
                throw;
            }
        }

        public JwtTokenGenerator(
            [ValidatedNotNull] RSA primaryPublicKeyRSA,
            [ValidatedNotNull] RSA primaryPrivateKeyRSA,
            [ValidatedNotNull] RSA secondaryPublicKeyRSA,
            [ValidatedNotNull] RSA secondaryPrivateKeyRSA,
            [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            Requires.NotNull(primaryPublicKeyRSA, nameof(primaryPublicKeyRSA));
            Requires.NotNull(primaryPrivateKeyRSA, nameof(primaryPrivateKeyRSA));
            Requires.NotNull(secondaryPublicKeyRSA, nameof(secondaryPublicKeyRSA));
            Requires.NotNull(secondaryPrivateKeyRSA, nameof(secondaryPrivateKeyRSA));
            Requires.NotNull(logger, nameof(logger));

            this.logger = logger;

            // primary key, for signing and validation
            try
            {
                this.primarySigningKid = Certificates.GenerateKidForPublicKey(primaryPublicKeyRSA);
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_primary_public_key");
                throw;
            }
            try
            {
                var key = GetSigningKey(primaryPrivateKeyRSA);
                this.primarySigningKey = new[] { key };
                this.primarySigningCredentials = GetRsaSigningCredentials(key);
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_primary_private_key");
                throw;
            }

            // secondary key, for validation purposes only
            try
            {
                this.secondarySigningKid = Certificates.GenerateKidForPublicKey(secondaryPublicKeyRSA);
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_secondary_public_key");
                throw;
            }
            try
            {
                var key = GetSigningKey(secondaryPrivateKeyRSA);
                this.secondarySigningKey = new[] { key };
            }
            catch (Exception e)
            {
                this.logger.WithValue("Exception", e.ToString()).LogError("error_read_secondary_private_key");
                throw;
            }
        }

        /// <summary>
        /// Returns a new JwtPayload instance with the basic fields filled out. In particular, it ensures to have an
        /// issuer, an audience, notBefore time, expires time, issuedAt time and a jti claim with a GUID.
        /// If the caller doesn't specify an expiration date, a default value of 1 hour in the future from now will
        /// be used.
        /// </summary>
        public JwtPayload NewJwtPayload(
            string issuer,
            string audience,
            string subject,
            DateTime expiration)
        {
            var utcNow = DateTime.UtcNow;
            var issuedAt = utcNow.Add(TokenIssuedAtClockSkew);
            var jwtid = Guid.NewGuid().ToString();

            this.logger.AddValue("jwtid", jwtid);

            return new JwtPayload(
                issuer: issuer,
                audience: audience,
                claims: new Claim[]
                {
                    new Claim("jti", jwtid),
                    new Claim("sub", subject),
                },
                notBefore: issuedAt,
                expires: expiration,
                issuedAt: issuedAt
            );
        }

        /// <summary>
        /// Takes a JwtPayload instance and creates a secure token signed with the configured certificate
        /// and indicating the currently configured "kid" in the header.
        /// </summary>
        public string WriteToken(JwtPayload payload)
        {
            var header = new JwtHeader(this.primarySigningCredentials)
            {
                { "kid", this.primarySigningKid }
            };

            var token = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Creates a signing credential out of an RS256 key
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