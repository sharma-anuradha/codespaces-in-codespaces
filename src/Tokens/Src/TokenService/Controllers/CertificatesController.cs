// <copyright file="CertificatesController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.TokenService.Contracts;
using Microsoft.VsSaaS.Tokens;
using static Microsoft.VsSaaS.Services.TokenService.Authentication.JwtBearerUtility;
using static Microsoft.VsSaaS.Services.TokenService.Utils;

namespace Microsoft.VsSaaS.Services.TokenService.Controllers
{
    /// <summary>
    /// API for public certificate operations.
    /// </summary>
    [ApiController]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("certificates_controller")]
    public class CertificatesController : Controller
    {
        private readonly IJwtReader jwtReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificatesController"/> class.
        /// </summary>
        /// <param name="jwtReader">Token reader from which certificates configuration
        /// will be read.</param>
        public CertificatesController(
            [FromServices]IJwtReader jwtReader)
        {
            this.jwtReader = Requires.NotNull(jwtReader, nameof(jwtReader));
        }

        /// <summary>
        /// Gets issuer public certificates.
        /// </summary>
        /// <param name="issuer">Optional issuer for which signing public certificates are
        /// requested.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An array of certificate result objects.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(CertificateSetResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("list")]
        public IActionResult List(
            [FromQuery]string? issuer,
            [FromServices]IDiagnosticsLogger logger)
        {
            var issuerCredentials = this.jwtReader.IssuerCredentials
                .Where((pair) => pair.Value != null);

            if (issuer != null)
            {
                issuerCredentials = issuerCredentials.Where(
                    (pair) => EqualsIgnoringTrailingSlash(pair.Key, issuer));

                if (!issuerCredentials.Any())
                {
                    logger.LogError("certificates_issuer_not_found");
                    return NotFound();
                }
            }

            var results = issuerCredentials
                .SelectMany((pair) => pair.Value.Select((c, i) => CreateCertificateResult(
                    issuer == null ? pair.Key : null, c, isPrimary: i == 0)))
                .Where((c) => c != null)
                .ToArray();

            return Ok(new CertificateSetResult { Certificates = results! });
        }

        private static CertificateResult? CreateCertificateResult(
            string? issuer, SigningCredentials credentials, bool isPrimary)
        {
            var certificate = (credentials as X509SigningCredentials)?.Certificate;
            if (certificate == null)
            {
                return null;
            }

            var result = new CertificateResult
            {
                Issuer = issuer,
                IsPrimary = isPrimary,
                KeyId = certificate.Thumbprint,
                Thumbprint = certificate.Thumbprint,
                PublicCertificate = new[]
                {
                    Convert.ToBase64String(certificate.Export(X509ContentType.Cert)),
                },
            };

            var key = certificate.PublicKey.Key as RSA;
            if (key != null)
            {
                var parameters = key.ExportParameters(false);
                result.Modulus = Base64UrlEncode(parameters.Modulus);
                result.Exponent = Base64UrlEncode(parameters.Exponent);
            }

            return result;
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value).TrimEnd('=').Replace('/', '_').Replace('+', '-');
        }
    }
}
