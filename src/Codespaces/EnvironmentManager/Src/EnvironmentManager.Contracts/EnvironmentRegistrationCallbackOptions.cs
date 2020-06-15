// <copyright file="EnvironmentRegistrationCallbackOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Options for an environment registraton callback.
    /// </summary>
    public class EnvironmentRegistrationCallbackOptions
    {
        /// <summary>
        /// Gets or sets the callback payload type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the callback payload options.
        /// </summary>
        public EnvironmentRegistrationCallbackPayloadOptions Payload { get; set; }
    }
}
