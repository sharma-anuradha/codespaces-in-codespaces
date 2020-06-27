// <copyright file="OpenIdMetadataController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.TokenService.Contracts;
using Microsoft.VsSaaS.Services.TokenService.Settings;

namespace Microsoft.VsSaaS.Services.TokenService.Controllers
{
    /// <summary>
    /// Handles requests for OpenId Connect provider metadata.
    /// </summary>
    /// <remarks>
    /// Implements the minimum required properties according to the spec:
    /// https://openid.net/specs/openid-connect-discovery-1_0.html .
    /// </remarks>
    [ApiController]
    [LoggingBaseName("openid_metadata_controller")]
    public class OpenIdMetadataController : Controller
    {
        private static readonly string[] ResponseTypesSupported = new[] { "code", "token" };
        private static readonly string[] SubjectTypesSupported = new[] { "public" };
        private static readonly string[] TokenSigningAlgorithmsSupported = new[] { "RS256" };

        private readonly TokenServiceAppSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenIdMetadataController"/> class.
        /// </summary>
        /// <param name="settings">Token service app settings.</param>
        public OpenIdMetadataController(
            [FromServices]TokenServiceAppSettings settings)
        {
            this.settings = Requires.NotNull(settings, nameof(settings));
        }

        /// <summary>
        /// Gets OpenId Connect provider metadata for an issuer.
        /// </summary>
        /// <param name="issuer">Issuer short name (not URI) matching one of the
        /// configured issuers in settings.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>Metadata result.</returns>
        [Route("/{issuer}/.well-known/openid-configuration")]
        [HttpGet]
        [ProducesResponseType(typeof(OpenIdMetadataResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("get")]
        public IActionResult Get(
            [FromRoute]string issuer,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (!this.settings.IssuerSettings.TryGetValue(issuer, out var issuerSettings))
            {
                return NotFound();
            }

            var serviceUri = $"{Request.Scheme}://{Request.Host}/";
            var issuerUri = issuerSettings.IssuerUri;

            var result = new OpenIdMetadataResult
            {
                Issuer = issuerUri,
                KeysEndpoint = serviceUri +
                    $"api/v1/certificates?issuer={HttpUtility.UrlEncode(issuerUri)}",
                ResponseTypesSupported = ResponseTypesSupported,
                SubjectTypesSupported = SubjectTypesSupported,
                TokenSigningAlgorithmsSupported = TokenSigningAlgorithmsSupported,

                // Not implemented (but the property is required)
                AuthorizationEndpoint = serviceUri + "oauth2/authorize",
            };

            return Ok(result);
        }
    }
}
