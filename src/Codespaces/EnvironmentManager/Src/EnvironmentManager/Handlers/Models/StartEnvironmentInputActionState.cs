// <copyright file="StartEnvironmentInputActionState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models
{
    /// <summary>
    /// Start environment continuation state.
    /// </summary>
    public enum StartEnvironmentInputActionState
    {
        /// <summary>
        /// Set up enviornment for creating new environment.
        /// </summary>
        CreateNew = 0,

        /// <summary>
        /// Set up enviornment for resuming.
        /// </summary>
        Resume = 1,

        /// <summary>
        /// Set up enviornment for exporting.
        /// </summary>
        Export = 2,
    }
}