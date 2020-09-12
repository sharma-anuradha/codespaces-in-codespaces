// <copyright file="StartEnvironmentContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Start compute continuation input.
    /// </summary>
    public class StartEnvironmentContinuationInput : BaseStartEnvironmentContinuationInput
    {
        /// <summary>
        /// Gets or sets the devcontainer JSON.
        /// </summary>
        public string DevContainer { get; set; }
    }
}
