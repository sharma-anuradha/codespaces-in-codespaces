// <copyright file="ValidateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Result from validating a token.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ValidateResult
    {
        /// <summary>
        /// Gets or sets the claims from the validated token, if validation was successful.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IDictionary<string, string>? Claims { get; set; }

        /// <summary>
        /// Gets or sets the validation error code, if validation was unsuccessful.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ValidationError Error { get; set; } = default;

        /// <summary>
        /// Gets or sets the validation error message, if validation was unsuccessful.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Converts a security token exception to an error result.
        /// </summary>
        /// <param name="ex">Security token exception.</param>
        /// <returns>Validation result with error details from the exception.</returns>
        public static ValidateResult FromException(SecurityTokenException ex)
        {
            var result = new ValidateResult
            {
                ErrorMessage = ex.Message,
            };

            if (ex is SecurityTokenSignatureKeyNotFoundException)
            {
                result.Error = ValidationError.SignatureKeyNotFound;
            }
            else if (ex is SecurityTokenInvalidSignatureException)
            {
                result.Error = ValidationError.InvalidSignature;
            }
            else if (ex is SecurityTokenInvalidIssuerException)
            {
                result.Error = ValidationError.InvalidIssuer;
            }
            else if (ex is SecurityTokenInvalidAudienceException)
            {
                result.Error = ValidationError.InvalidAudience;
            }
            else if (ex is SecurityTokenDecryptionFailedException)
            {
                result.Error = ValidationError.DecryptionFailed;
            }
            else if (ex is SecurityTokenExpiredException)
            {
                result.Error = ValidationError.Expired;
            }
            else
            {
                result.Error = ValidationError.Unknown;
            }

            return result;
        }

        /// <summary>
        /// Converts a validation error result object to an exception.
        /// </summary>
        /// <returns>Security token exception derived from the validation result.</returns>
        public SecurityTokenException ToException()
        {
            var message = ErrorMessage ?? "Unknown token validation error.";
            switch (Error)
            {
                case ValidationError.SignatureKeyNotFound:
                    return new SecurityTokenSignatureKeyNotFoundException(message);
                case ValidationError.InvalidSignature:
                    return new SecurityTokenInvalidSignatureException(message);
                case ValidationError.InvalidIssuer:
                    return new SecurityTokenInvalidIssuerException(message);
                case ValidationError.InvalidAudience:
                    return new SecurityTokenInvalidAudienceException(message);
                case ValidationError.DecryptionFailed:
                    return new SecurityTokenDecryptionFailedException(message);
                case ValidationError.Expired:
                    return new SecurityTokenExpiredException(message);
                default: return new SecurityTokenValidationException(message);
            }
        }
    }
}
