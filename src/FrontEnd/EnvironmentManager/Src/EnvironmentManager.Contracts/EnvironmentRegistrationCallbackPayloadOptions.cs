// <copyright file="EnvironmentRegistrationCallbackPayloadOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment registration callback payload options.
    /// </summary>
    public class EnvironmentRegistrationCallbackPayloadOptions
    {
        /// <summary>
        /// Gets or sets Live Share session id.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the Live Share session path.
        /// </summary>
        public string SessionPath { get; set; }
    }
}
