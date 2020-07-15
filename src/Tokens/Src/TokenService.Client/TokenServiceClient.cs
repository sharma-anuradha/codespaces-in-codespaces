// <copyright file="TokenServiceClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.TokenService.Contracts;

namespace Microsoft.VsSaaS.Services.TokenService.Client
{
    /// <summary>
    /// Client for token service.
    /// </summary>
    public class TokenServiceClient : IDisposable
    {
        private const string VisualStudioServicesApiAppId = "9bd5ab7f-4031-4045-ace9-6bebbad202f6";
        private const string CertificatesApiPath = "/api/v1/certificates";
        private const string TokensApiPath = "/api/v1/tokens";

        private static readonly MediaTypeFormatter[] Formatters = CreateFormatters();

        private readonly HttpClient httpClient;
        private readonly Func<Task<AuthenticationHeaderValue?>> authCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenServiceClient"/> class
        /// with an HTTP client and no client authentication callback.
        /// </summary>
        /// <param name="httpClient">HttpClient with base address set to the token service
        /// (not including any API path).</param>
        /// <remarks>
        /// Only some of the token service APIs can be called with no authentication.
        /// </remarks>
        public TokenServiceClient(HttpClient httpClient)
        {
            this.httpClient = Requires.NotNull(httpClient, nameof(httpClient));
            this.authCallback = () => Task.FromResult<AuthenticationHeaderValue?>(null);

            if (httpClient.BaseAddress == null)
            {
                throw new ArgumentException(
                    "A base address is required.",
                    $"{nameof(httpClient)}.{nameof(HttpClient.BaseAddress)}");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenServiceClient"/> class
        /// with an HTTP client and a client authentication callback.
        /// </summary>
        /// <param name="httpClient">HttpClient with base address set to the token service
        /// (not including any API path).</param>
        /// <param name="authCallback">Async callback for retrieving a client auth token.</param>
        public TokenServiceClient(
            HttpClient httpClient,
            Func<Task<AuthenticationHeaderValue?>> authCallback)
            : this(httpClient)
        {
            this.authCallback = Requires.NotNull(authCallback, nameof(authCallback));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenServiceClient"/> class
        /// with an HTTP client and client identity parameters.
        /// </summary>
        /// <param name="httpClient">HttpClient with base address set to the token service
        /// (not including any API path).</param>
        /// <param name="servicePrincipalIdentity">Identity of the service principal that is
        /// acting as a client of the token service.</param>
        public TokenServiceClient(
            HttpClient httpClient,
            ServicePrincipalIdentity servicePrincipalIdentity)
            : this(httpClient)
        {
            Requires.NotNull(servicePrincipalIdentity, nameof(servicePrincipalIdentity));

            this.authCallback = () =>
                servicePrincipalIdentity.GetAuthenticationHeaderAsync(VisualStudioServicesApiAppId);
        }

        /// <summary>
        /// Converts a token service HTTP response to a result object (or exception).
        /// </summary>
        /// <typeparam name="T">Type of result expected.</typeparam>
        /// <param name="response">Response from a token service request.</param>
        /// <param name="allowNotFound">True if 404 Not Found is a valid response.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>Result object of the requested type, or null if the response is Not Found
        /// and <paramref name="allowNotFound"/> is true.</returns>
        /// <exception cref="ArgumentException">The service returned a
        /// 400 Bad Request response.</exception>
        /// <exception cref="UnauthorizedAccessException">The service return a 401 Unauthorized
        /// or 403 Forbidden response.</exception>
        public static async Task<T?> ConvertResponseAsync<T>(
            HttpResponseMessage response,
            bool allowNotFound,
            CancellationToken cancellation)
            where T : class
        {
            Requires.NotNull(response, nameof(response));

            string? errorMessage = null;
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    T result = await response.Content.ReadAsAsync<T>(Formatters, cancellation);
                    return result;
                }
                catch (Exception ex)
                {
                    errorMessage = "Token service response deserialization error: " + ex.Message;
                }
            }

            if (errorMessage == null && response.Content != null)
            {
                try
                {
                    if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                    else if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        // 4xx status responses may include standard ProblemDetails.
                        var problemDetails = await response.Content.ReadAsAsync<ProblemDetails>(
                            Formatters, cancellation);
                        if (!string.IsNullOrEmpty(problemDetails?.Title) ||
                            !string.IsNullOrEmpty(problemDetails?.Detail))
                        {
                            errorMessage = "Token service error: " +
                                problemDetails!.Title + " " + problemDetails.Detail;
                        }
                    }
                    else if ((int)response.StatusCode >= 500)
                    {
                        // 5xx status responses may include VS SaaS error details.
                        var errorDetails = await response.Content.ReadAsAsync<ErrorDetails>(
                            Formatters, cancellation);
                        if (!string.IsNullOrEmpty(errorDetails?.Message))
                        {
                            errorMessage = "Token service error: " + errorDetails!.Message;
                            if (!string.IsNullOrEmpty(errorDetails.StackTrace))
                            {
                                errorMessage += "\n" + errorDetails.StackTrace;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Swallow exceptions from reading error details.
                    // A default error message will be filled in below.
                }
            }

            errorMessage ??= "Token service response status code: " + response.StatusCode;

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new ArgumentException(errorMessage);
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(errorMessage);
            }
            else
            {
                throw new Exception(errorMessage);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        /// <summary>
        /// Issues a new JWT token via the token service.
        /// </summary>
        /// <param name="claims">Claims for the token to be issued.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>The issued JWT token.</returns>
        /// <exception cref="ArgumentException">Some claims were missing or invalid. The exception
        /// message often contains details.</exception>
        /// <exception cref="UnauthorizedAccessException">The client authentication token was
        /// missing or invalid, or the client is not authorized to issue tokens with the specified
        /// issuer or audience.</exception>
        public async Task<string> IssueAsync(
            JwtPayload claims,
            CancellationToken cancellation)
        {
            Requires.NotNull(claims, nameof(claims));

            var requestParameters = new IssueParameters
            {
                Claims = claims,
            };

            this.httpClient.DefaultRequestHeaders.Authorization = await this.authCallback();
            var response = await this.httpClient.PostAsJsonAsync(
                TokensApiPath + "/issue",
                requestParameters,
                cancellation);

            var result = await ConvertResponseAsync<IssueResult>(
                response,
                allowNotFound: false,
                cancellation);
            return result!.Token;
        }

        /// <summary>
        /// Validates a JWT token via the token service, decrypting if necessary, and returns the
        /// resulting claims; throws an exception if the token is invalid.
        /// </summary>
        /// <param name="token">Token to be validated.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>Claims from the validated token.</returns>
        /// <exception cref="ArgumentException">The token is missing or unparseable.</exception>
        /// <exception cref="UnauthorizedAccessException">The client authentication token was
        /// missing or invalid, or the token is encrypted and the client is not authorized to
        /// decrypt it.</exception>
        /// <exception cref="SecurityTokenSignatureKeyNotFoundException">The token was signed
        /// using an unknown key or certificate.</exception>
        /// <exception cref="SecurityTokenInvalidSignatureException">The token signature is
        /// invalid.</exception>
        /// <exception cref="SecurityTokenInvalidIssuerException">The token issuer is not one of
        /// the issuers known by the service.</exception>
        /// <exception cref="SecurityTokenInvalidAudienceException">The token audience is
        /// not one of the audiences supported by the service.</exception>
        /// <exception cref="SecurityTokenExpiredException">The token is expired.</exception>
        /// <exception cref="SecurityTokenDecryptionFailedException">The token could not be
        /// decrypted by the service.</exception>
        /// <exception cref="SecurityTokenValidationException">The token was invalid for some
        /// other reason.</exception>
        public async Task<JwtPayload> ValidateAsync(
            string token,
            CancellationToken cancellation)
        {
            Requires.NotNull(token, nameof(token));

            var requestParameters = new ValidateParameters
            {
                Token = token,
            };

            this.httpClient.DefaultRequestHeaders.Authorization = await this.authCallback();
            var response = await this.httpClient.PostAsJsonAsync(
                TokensApiPath + "/validate",
                requestParameters,
                cancellation);

            var result = await ConvertResponseAsync<ValidateResult>(
                response,
                allowNotFound: false,
                cancellation);

            if (result!.Claims == null)
            {
                throw result.ToException();
            }

            var resultPayload = new JwtPayload(result.Claims.Select(
                (claim) => new System.Security.Claims.Claim(claim.Key, claim.Value)));
            return resultPayload;
        }

        /// <summary>
        /// Exchanges a token for a newly issued JWT token via the token service.
        /// </summary>
        /// <param name="audience">Optional requested audience for the resulting token.</param>
        /// <param name="lifetime">Optional requested lifetime for the resulting token.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>The issued JWT token.</returns>
        /// <exception cref="ArgumentException">Some claims or parameters were missing or invalid.
        /// The exception message often contains details.</exception>
        /// <exception cref="UnauthorizedAccessException">The client authentication token was
        /// missing or invalid.</exception>
        /// <remarks>
        /// The input token for the exchange is supplied via the client auth callback (or service
        /// principal identity) passed into the constructor.
        /// <para />
        /// If the audience is unspecified, the configured default audience will be used.
        /// <para />
        /// If the requested lifetime is greater than the configured maximum, the maximum is used.
        /// If the lifetime is unspecified, the configured default lifetime will be used.
        /// </remarks>
        public async Task<string> ExchangeAsync(
            string? audience,
            TimeSpan? lifetime,
            CancellationToken cancellation)
        {
            ExchangeParameters? requestParameters = null;
            if (audience != null || lifetime != null)
            {
                requestParameters = new ExchangeParameters
                {
                    Audience = audience,
                    Lifetime = lifetime,
                };
            }

            this.httpClient.DefaultRequestHeaders.Authorization = await this.authCallback();
            var response = await this.httpClient.PostAsJsonAsync(
                TokensApiPath + "/exchange",
                requestParameters,
                cancellation);

            var result = await ConvertResponseAsync<IssueResult>(
                response,
                allowNotFound: false,
                cancellation);
            return result!.Token;
        }

        /// <summary>
        /// Gets public certificates for the issuer.
        /// </summary>
        /// <param name="issuer">Issuer URI.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>Collection of certificates for the issuer (where the first is primary)
        /// or null if the issuer is not found.</returns>
        public async Task<IEnumerable<X509Certificate2>?> GetIssuerPublicCertificatesAsync(
            string issuer,
            CancellationToken cancellation)
        {
            Requires.NotNullOrEmpty(issuer, nameof(issuer));

            this.httpClient.DefaultRequestHeaders.Authorization = await this.authCallback();

            var query = issuer == null ? string.Empty : $"?issuer={HttpUtility.UrlEncode(issuer)}";
            var response = await this.httpClient.GetAsync(
                CertificatesApiPath + query,
                cancellation);

            var result = await ConvertResponseAsync<CertificateSetResult>(
                response,
                allowNotFound: true,
                cancellation);

            if (result == null)
            {
                return null;
            }

            return result.Certificates
                .OrderBy((c) => c.IsPrimary ? 0 : 1)
                .Select((c) => new X509Certificate2(
                    Convert.FromBase64String(c.PublicCertificate?.First())));
        }

        private static MediaTypeFormatter[] CreateFormatters()
        {
            var jsonFormatter = new JsonMediaTypeFormatter();
            jsonFormatter.SupportedMediaTypes.Add(
                new MediaTypeHeaderValue("application/problem+json"));
            return new[] { jsonFormatter };
        }
    }
}
