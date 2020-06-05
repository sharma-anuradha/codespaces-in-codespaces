// <copyright file="ShutdownEnvironmentContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models
{
    /// <summary>
    /// Shutdown environment continuation input.
    /// </summary>
    public class ShutdownEnvironmentContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Gets or sets current state.
        /// </summary>
        public ShutdownEnvironmentContinuationInputState CurrentState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the environment should be force suspended.
        /// </summary>
        public bool Force { get; set; }
    }
}
