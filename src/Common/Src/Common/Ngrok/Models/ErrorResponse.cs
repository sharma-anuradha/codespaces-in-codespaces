// <copyright file="ErrorResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models
{
    /// <summary>
    /// Ngrok Error Response.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets the Ngrok Error Code.
        /// </summary>
        [JsonProperty(PropertyName = "error_code")]
        public int NgrokErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the HTTP Status Code of the given request.
        /// </summary>
        [JsonProperty(PropertyName = "status_code")]
        public int HttpStatusCode { get; set; }

        /// <summary>
        /// Gets or sets the Ngrok Error Message.
        /// </summary>
        [JsonProperty(PropertyName = "msg")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the details of the error message.
        /// </summary>
        [JsonProperty(PropertyName = "details")]
        public ErrorDetails Details { get; set; }
    }
}
