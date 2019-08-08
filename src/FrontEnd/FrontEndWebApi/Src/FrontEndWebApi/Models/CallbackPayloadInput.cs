// <copyright file="CallbackPayloadInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// The environment registration callback payload.
    /// </summary>
    public class CallbackPayloadInput
    {
        /// <summary>
        /// Gets or sets the environment connection session id.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the environment connection session path.
        /// </summary>
        public string SessionPath { get; set; }
    }
}
