// <copyright file="ProblemDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Structure of error details returned by the token service, including validation errors.
    /// </summary>
    /// <remarks>
    /// This object may be returned with a response status code of 400 (or other 4xx code).
    ///
    /// Compatible with RFC 7807 Problem Details (https://tools.ietf.org/html/rfc7807) and
    /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails
    /// but doesn't require adding a dependency on that package.
    /// </remarks>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ProblemDetails
    {
        /// <summary>
        /// Gets or sets the error title.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the error detail.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Detail { get; set; }
    }
}
