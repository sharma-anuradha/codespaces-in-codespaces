// <copyright file="ErrorDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models
{
    /// <summary>
    /// Ngrok Error Details.
    /// </summary>
    public class ErrorDetails
    {
        /// <summary>
        /// Gets or sets the Ngrok detailed error message.
        /// </summary>
        [JsonProperty(PropertyName = "err")]
        public string DetailedErrorMessage { get; set; }
    }
}
