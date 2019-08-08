// <copyright file="CallbackOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Options for an environment registraton callback.
    /// </summary>
    public class CallbackOptions
    {
        /// <summary>
        /// Gets or sets the environment type.
        /// </summary>
        public CloudEnvironmentType Type { get; set; }

        /// <summary>
        /// Gets or sets the callback payload options.
        /// </summary>
        public CallbackPayloadOptions Payload { get; set; }
    }
}
