// <copyright file="ResourceProviderErrorResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// The error class used by the ResourceProvider to send properly formatted responses back to RPaaS.
    /// </summary>
    public class ResourceProviderErrorResponse
    {
        /// <summary>
        /// Gets or sets the status of the request.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the error object.
        /// </summary>
        public ResourceProviderErrorInfo Error { get; set; }

        /// <summary>
        /// Helper method to create a json error response from the input values.
        /// </summary>
        /// <param name="errorCode">The string represented error code.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="statusCode">The status code.</param>
        /// <returns>JsonResult.</returns>
        public static IActionResult Create(string errorCode, string errorMessage = default, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var errorResponse = new ResourceProviderErrorResponse
            {
                Error = new ResourceProviderErrorInfo
                {
                    Code = errorCode,
                    Message = errorMessage,
                },
                Status = "Failed",
            };

            return new JsonResult(errorResponse)
            {
                StatusCode = (int)statusCode,
            };
        }
    }
}
