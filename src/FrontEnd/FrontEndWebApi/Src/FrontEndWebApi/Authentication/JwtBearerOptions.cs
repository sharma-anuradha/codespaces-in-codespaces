// <copyright file="JwtBearerOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// Options for JWT tokens.
    /// </summary>
    public class JwtBearerOptions
    {
        /// <summary>
        /// Gets or sets the valid audiences.
        /// </summary>
        public string Audiences { get; set; }

        /// <summary>
        /// Gets or sets the token authority.
        /// </summary>
        public string Authority { get; set; }
    }
}
